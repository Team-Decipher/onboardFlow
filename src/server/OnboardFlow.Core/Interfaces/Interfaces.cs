namespace OnboardFlow.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}

public interface IBlobStorageService
{
    Task<string> UploadFileAsync(Stream stream, string fileName, string contentType);
    Task<Stream> DownloadFileAsync(string blobUri);
    Task DeleteFileAsync(string blobUri);
}

public interface INotificationService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}
