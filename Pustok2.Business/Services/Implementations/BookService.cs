using System.Linq.Expressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Pustok2.Business.Exceptions.CommonExceptions;
using Pustok2.Business.Services.Interfaces;
using Pustok2.Business.Utilities.Extensions;
using Pustok2.Business.ViewModels;
using Pustok2.Core.Models;
using Pustok2.Core.Repositories;

namespace Pustok2.Business.Services.Implementations
{
	public class BookService : IBookService
	{
		private readonly IWebHostEnvironment _env;
		private readonly IGenreRepository _genreRepository;
		private readonly IAuthorRepository _authorRepository;
		private readonly IBookImageRepository _bookImageRepository;
		private readonly IBookRepository _bookRepository;

		public BookService(IWebHostEnvironment env, IGenreRepository genreRepository, 
			IAuthorRepository authorRepository, IBookImageRepository bookImageRepository,
			IBookRepository bookRepository)
        {
			_env = env;
			_genreRepository = genreRepository;
			_authorRepository = authorRepository;
			_bookImageRepository = bookImageRepository;
			_bookRepository = bookRepository;
		}
        public async Task CreateAsync(CreateBookVM vm)
		{
			if (await _genreRepository.Table.AllAsync(x => x.Id != vm.GenreId))
			{
				throw new EntityNotFoundException("GenreId", "Genre not found");
			}

			if (await _authorRepository.Table.AllAsync(x => x.Id != vm.AuthorId))
			{
				throw new EntityNotFoundException("AuthorId", "author not found");
			}

			Book book = new Book()
			{
				Title = vm.Title,
				Description = vm.Description,
				StockCount = vm.StockCount,
				CostPrice = vm.CostPrice,
				SalePercent = vm.SalePercent,
				SalePrice = vm.SalePrice,
				IsAvailable = vm.IsAvailable,
				Code = vm.Code,
				AuthorId = vm.AuthorId,
				GenreId = vm.GenreId,
			};

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
					Book = book
				};

				await _bookImageRepository.CreateAsync(bookImage);
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
					Book = book
				};

				await _bookImageRepository.CreateAsync(bookImage);
			}

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
						Book = book
					};

					await _bookImageRepository.CreateAsync(bookImage);
				}
			}

			await _bookRepository.CreateAsync(book);
			await _bookRepository.CommitAsync();
		}

		public async Task DeleteAsync(int id, params string[] includes)
{
	var book = await _bookRepository.GetByIdAsync(id, includes);

	if (book == null)
    {
        throw new EntityNotFoundException("Book Not found");
    }

    foreach (var image in book.BookImages)
    {
        if (!string.IsNullOrEmpty(image.ImageUrl))
        {
            image.ImageUrl.DeleteFile(_env.WebRootPath, "uploads/books");
        }
    }

    _bookRepository.Delete(book);
    await _bookRepository.CommitAsync();

}

		public async Task<ICollection<Book>> GetAllAsync(Expression<Func<Book, bool>> expression, params string[] includes)
		{
			return await _bookRepository.GetAll(expression, includes).ToListAsync();
		}

        public async Task<Book> GetByExpressionAsync(Expression<Func<Book, bool>> expression, params string[] includes)
        {
			var data = await _bookRepository.GetByExpressionAsync(expression, includes);

            if (data is null)
            {
                throw new EntityNotFoundException("Book Not found");
            }

            return data;
        }

        public Task<Book> GetByIdAsync(int? id)
		{
			var data = _bookRepository.GetByIdAsync(id);

			if(data is null)
			{
				throw new EntityNotFoundException("Book Not found");
			}

			return data;
		}

		public Task UpdateAsync(int? id, UpdateBookVM vm)
		{

            throw new NotImplementedException();
        }
    }
}
