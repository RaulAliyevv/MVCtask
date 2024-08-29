using Pustok2.Business.ViewModels.Author;
using Pustok2.Business.ViewModels;
using Pustok2.Core.Models;
using System.Linq.Expressions;

namespace Pustok2.Business.Services.Interfaces
{
	public interface IBookService
	{
		Task CreateAsync(CreateBookVM vm);
		Task UpdateAsync(int? id, UpdateBookVM vm);
		Task<Book> GetByIdAsync(int? id);
		Task DeleteAsync(int id, params string[] includes);
		Task<ICollection<Book>> GetAllAsync(Expression<Func<Book, bool>> expression, params string[] includes);
		public Task<Book> GetByExpressionAsync(Expression<Func<Book, bool>> expression, params string[] includes);
    }
}
