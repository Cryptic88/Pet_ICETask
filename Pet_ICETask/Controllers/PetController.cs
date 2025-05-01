using System.Drawing;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pet_ICETask.Models;
using static System.Net.WebRequestMethods;

namespace Pet_ICETask.Controllers
{
    public class PetController : Controller
    {
        private readonly string connectionString = "DefaultEndpointsProtocol=https;AccountName=peticetask;AccountKey=yhSknYiGtgmjCnxsEp78hd9moQWbUcWj34KE+UJ85KrzX+IvF+P2tKgrBGeHsPq7fTcn3+tfIcYO+ASt5aRvdA==;EndpointSuffix=core.windows.net";
        private readonly string containerName = "pet";
        private readonly ApplicationDbContext _context;

        public PetController(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var imageUrls = await FetchImageUrlsAsync();
            return View(imageUrls);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile uploadFile)
        {
            if (uploadFile != null && uploadFile.Length > 0)
            {
                var fileUrl = await UploadFileToBlobStorageAsync(uploadFile);
                if (string.IsNullOrEmpty(fileUrl))
                {
                    // Log or return an error if the URL is empty
                    ModelState.AddModelError(string.Empty, "Image upload failed.");
                    return View("Index"); // Return to Index or display an error page
                }

                // Create a new PetForm with the image URL
                var viewModel = new PetForm { Pet = new Pet { ImageUrl = fileUrl } };

                return RedirectToAction("AddPetDetails", new { imageUrl = fileUrl });
            }

            // Handle file upload failure
            ModelState.AddModelError(string.Empty, "Please upload a valid image.");
            return View("Index"); // Return to Index or display an error page
        }

        [HttpGet]
        public IActionResult AddPetDetails(string imageUrl)
        {
            var viewModel = new PetForm
            {
                ImageUrl = imageUrl,
                Pet = new Pet { ImageUrl = imageUrl }
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddPetDetails(PetForm viewModel)
        {
            if (ModelState.IsValid)
            {
                    viewModel.Pet.ImageUrl = viewModel.ImageUrl;
                    _context.Pets.Add(viewModel.Pet);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("ViewAllPets");

            }

            return View(viewModel);
        }

        public async Task<IActionResult> ViewAllPets()
        {
            ViewData["Theme"] = "superhero-theme";
            var pets = await _context.Pets.ToListAsync();
            return View(pets);
        }

        private async Task<List<string>> FetchImageUrlsAsync()
        {
            var imageUrls = new List<string>();
            var containerClient = new BlobContainerClient(connectionString, containerName);

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                imageUrls.Add(blobClient.Uri.ToString());
            }

            return imageUrls;
        }

        private async Task<string> UploadFileToBlobStorageAsync(IFormFile uploadFile)
        {
            var containerClient = new BlobContainerClient(connectionString, containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(uploadFile.FileName);
            using (var stream = uploadFile.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            return blobClient.Uri.ToString(); // Return URL to redirect with
        }

        public async Task<IActionResult> ViewPet(int id)
        {
            var pet = await _context.Pets.FindAsync(id);
            if (pet == null)
            {
                return NotFound();
            }

            ViewBag.FunFact = GetFunFact(pet.Breed);
            return View(pet);
        }

        public async Task<IActionResult> RandomPet()
        {
            var pets = await _context.Pets.ToListAsync();
            if (pets == null || !pets.Any())
            {
                return View("Error", "No pets found.");
            }

            var random = new Random();
            var randomPet = pets[random.Next(pets.Count)];

            return View("RandomPet", randomPet);
        }

        public string GetFunFact(string breed)
        {
            return breed switch
            {
                "Golden Retriever" => "Golden Retrievers are known for their friendly nature and love of water!",
                "Bulldog" => "Bulldogs were originally bred for bull-baiting.",
                "Rottweiler" => "Rottweilers were originally Roman drover dogs, used to herd cattle and pull carts!",
                "Siamese Cat" => "Siamese cats are highly social and vocal, often 'talking' to their owners with a unique voice!"
            };
        }
    }
}
