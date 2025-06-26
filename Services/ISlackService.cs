using System.Threading.Tasks;

namespace GiddhTemplate.Services
{
    public interface ISlackService
    {
        Task SendErrorAlertAsync(string url, string environment, string error, string stackTrace);
    }
}