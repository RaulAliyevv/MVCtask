namespace Pustok2.Business.Exceptions.CommonExceptions
{
	public class FileValidationException : Exception
	{
		public string PropName { get; set; }
		public FileValidationException()
		{
		}

		public FileValidationException(string? message) : base(message)
		{
		}

		public FileValidationException(string propname, string? message) : base(message)
		{
			PropName = propname;
		}
	}
}
