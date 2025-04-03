using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using MyASCS.Models;

namespace MyASCS.Services.Interfaces;

public interface IPersonIdentificationService
{
        Task<PersonModel> IdentifyPersonAsync(Bitmap frame);
}