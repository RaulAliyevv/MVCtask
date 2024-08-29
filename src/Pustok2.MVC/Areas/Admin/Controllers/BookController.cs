using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Pustok2.Business.Exceptions.CommonExceptions;
using Pustok2.Business.Services.Interfaces;
using Pustok2.Business.Utilities.Extensions;
using Pustok2.Business.ViewModels;
using Pustok2.Core.Models;
using Pustok2.Core.Repositories;

namespace Pustok2.MVC.Areas.Admin.Controllers;

[Area("Admin")]
public class BookController : Controller
{
    private readonly IGenreService _genreService;
    private readonly IAuthorService _authorService;
    private readonly IWebHostEnvironment _env;
    private readonly IBookRepository _bookRepository;
    private readonly IGenreRepository _genreRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly IBookImageRepository _bookImageRepository;
    private readonly IBookService _bookService;

    public BookController(IGenreService genreService, IAuthorService authorService,
                            IWebHostEnvironment env, IBookRepository bookRepository,
                            IGenreRepository genreRepository, IAuthorRepository authorRepository,
                            IBookImageRepository bookImageRepository, IBookService bookService)
    {
        _genreService = genreService;
        _authorService = authorService;
        _env = env;
        _bookRepository = bookRepository;
        _genreRepository = genreRepository;
        _authorRepository = authorRepository;
        _bookImageRepository = bookImageRepository;
        _bookService = bookService;
    }

    public async Task<IActionResult> Index()
    {
        var books = await _bookRepository.Table
                                      .Include(b => b.Genre)
                                      .Include(b => b.Author)
                                      .Include(b => b.BookImages)
                                      .Where(b => !b.IsDeleted)
                                      .ToListAsync();
        return View(books);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Genres = await _genreService.GetAllAsync(x => !x.IsDeleted);
        ViewBag.Authors = await _authorService.GetAllAsync(x => !x.IsDeleted);

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookVM vm)
    {
        ViewBag.Genres = await _genreService.GetAllAsync(x => !x.IsDeleted);
        ViewBag.Authors = await _authorService.GetAllAsync(x => !x.IsDeleted);

        if (!ModelState.IsValid) return View(vm);

        try
        {
            await _bookService.CreateAsync(vm);
        }
        catch (EntityNotFoundException ex)
        {
            ModelState.AddModelError(ex.PropName, ex.Message);
            return View();
        }
        catch (FileValidationException ex)
        {
            ModelState.AddModelError(ex.PropName, ex.Message);
            return View();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View();
        }

        await _bookRepository.CommitAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Update(int id)
    {
        ViewBag.Genres = await _genreService.GetAllAsync(x => !x.IsDeleted);
        ViewBag.Authors = await _authorService.GetAllAsync(x => !x.IsDeleted);

        Book data = null;

        try
        {
            data = await _bookService.GetByExpressionAsync(x => x.Id == id, "BookImages", "Author", "Genre");
        }
        catch (EntityNotFoundException)
        {
            return View("ErrorPage");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View();
        }

        UpdateBookVM vm = new UpdateBookVM()
        {
            Title = data.Title,
            Description = data.Description,
            StockCount = data.StockCount,
            CostPrice = data.CostPrice,
            SalePercent = data.SalePercent,
            SalePrice = data.SalePrice,
            IsAvailable = data.IsAvailable,
            Code = data.Code,
            AuthorId = data.AuthorId,
            GenreId = data.GenreId,
            BookImages = data.BookImages,
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int? id, UpdateBookVM vm)
    {
        ViewBag.Genres = await _genreService.GetAllAsync(x => !x.IsDeleted);
        ViewBag.Authors = await _authorService.GetAllAsync(x => !x.IsDeleted);

        if (id is null || id <= 0)
        {
            return View("ErrorPage");
        }

        if (!ModelState.IsValid) return View();

        var existdata = await _bookService.GetByExpressionAsync(x => x.Id == id, "BookImages", "Author", "Genre");
        if (existdata is null)
        {
            return View("ErrorPage");
        }

        existdata.Title = vm.Title;
        existdata.Description = vm.Description;
        existdata.StockCount = vm.StockCount;
        existdata.CostPrice = vm.CostPrice;
        existdata.SalePercent = vm.SalePercent;
        existdata.SalePrice = vm.SalePrice;
        existdata.IsAvailable = vm.IsAvailable;
        existdata.Code = vm.Code;
        existdata.AuthorId = vm.AuthorId;
        existdata.GenreId = vm.GenreId;

        if (vm.PosterImage is not null)
        {
            if (vm.PosterImage.ContentType != "image/jpeg" && vm.PosterImage.ContentType != "image/png")
            {
                throw new FileValidationException("PosterImage", "posterimage type must be png, jpeg");
            }
            if (vm.PosterImage.Length > 2097152)
            {
                throw new FileValidationException("PosterImage", "posterimage size must be <= 2mb");
            }

            BookImage bookImage = new BookImage()
            {
                ImageUrl = vm.PosterImage.CreateFileAsync(_env.WebRootPath, "uploads/books"),
                Createdtime = DateTime.Now,
                Updatedtime = DateTime.Now,
                IsDeleted = false,
                IsPrimary = true,
                BookId = existdata.Id
            };

            await _bookImageRepository.CreateAsync(bookImage);

            _bookImageRepository.Delete(existdata.BookImages.FirstOrDefault(x => x.IsPrimary == true));
            existdata.BookImages.FirstOrDefault(x => x.IsPrimary == true)?.ImageUrl.DeleteFile(_env.WebRootPath, "uploads/books");
        }

        if (vm.HoverImage is not null)
        {
            if (vm.HoverImage.ContentType != "image/jpeg" && vm.HoverImage.ContentType != "image/png")
            {
                throw new FileValidationException("HoverImage", "posterimage type must be png, jpeg");
            }
            if (vm.HoverImage.Length > 2097152)
            {
                throw new FileValidationException("HoverImage", "posterimage size must be <= 2mb");
            }

            BookImage bookImage = new BookImage()
            {
                ImageUrl = vm.HoverImage.CreateFileAsync(_env.WebRootPath, "uploads/books"),
                Createdtime = DateTime.Now,
                Updatedtime = DateTime.Now,
                IsDeleted = false,
                IsPrimary = false,
                BookId = existdata.Id
            };

            await _bookImageRepository.CreateAsync(bookImage);

            _bookImageRepository.Delete(existdata.BookImages.FirstOrDefault(x => x.IsPrimary == false));
            existdata.BookImages.FirstOrDefault(x => x.IsPrimary == false)?.ImageUrl.DeleteFile(_env.WebRootPath, "uploads/books");
        }

        if(vm.BookImageIds is not null)
        {
            foreach (var item in existdata.BookImages.Where(bi => !vm.BookImageIds.Exists(bid => bi.Id == bid) && bi.IsPrimary == null))
            {
                item.ImageUrl.DeleteFile(_env.WebRootPath, "uploads/books");
            }

            existdata.BookImages.RemoveAll(bi => !vm.BookImageIds.Exists(bid => bi.Id == bid) && bi.IsPrimary == null);
        }
        else
        {
            foreach (var item in existdata.BookImages.Where(bi => bi.IsPrimary == null))
            {
                item.ImageUrl.DeleteFile(_env.WebRootPath, "uploads/books");
            }

            existdata.BookImages.RemoveAll(bi => bi.IsPrimary == null);
        }

        if (vm.Images is not null)
        {
            if (vm.Images.Count > 0)
            {
                foreach (var img in vm.Images)
                {
                    if (img.ContentType != "image/jpeg" && img.ContentType != "image/png")
                    {
                        throw new FileValidationException("Images", "posterimage type must be png, jpeg");
                    }
                    if (img.Length > 2097152)
                    {
                        throw new FileValidationException("Images", "posterimage size must be <= 2mb");
                    }

                    BookImage bookImage = new BookImage()
                    {
                        ImageUrl = img.CreateFileAsync(_env.WebRootPath, "uploads/books"),
                        Createdtime = DateTime.Now,
                        Updatedtime = DateTime.Now,
                        IsDeleted = false,
                        IsPrimary = null,
                        BookId = existdata.Id
                    };

                    await _bookImageRepository.CreateAsync(bookImage);
                }

            }
        }

        await _bookRepository.CommitAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {

        await _bookService.DeleteAsync(id, "BookImages");
        return RedirectToAction(nameof(Index));
    }

}


