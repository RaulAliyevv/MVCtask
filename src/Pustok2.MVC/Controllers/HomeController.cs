using Microsoft.AspNetCore.Mvc;
using Pustok2.Business.Services.Interfaces;
using Pustok2.MVC.ViewModels;

namespace Pustok2.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISlideService _slideService;
        private readonly IBookService _bookService;

        public HomeController(ISlideService slideService, IBookService bookService)
        {
            _slideService = slideService;
            _bookService = bookService;
        }

        public async Task<IActionResult> Index()
        {
            var slides = await _slideService.GetAllAsync();
            var books = await _bookService.GetAllAsync(null, "BookImages", "Author", "Genre");

            var homeVM = new HomeVM
            {
                Slides = slides.ToList(),
                Books = books.ToList(),
            };

            return View(homeVM);
        }
    }
}
