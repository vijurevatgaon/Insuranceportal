using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using InsuranceClient.Models;
using InsuranceClient.Models.ViewModels;
using System.IO;
using Microsoft.Extensions.Configuration;
using InsuranceClient.Helpers;

namespace InsuranceClient.Controllers
{
    public class HomeController : Controller
    {


        private IConfiguration configuration;
        public HomeController(IConfiguration _configuration)
        {
            this.configuration = _configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (ModelState.IsValid)
            {

                var customerid = Guid.NewGuid();
                StorageHelper storageHelper = new StorageHelper();
                storageHelper.ConnectionString = configuration.GetConnectionString("StorageConnection");

                //Save Customer image to Azure BLOB
                var tempFile = Path.GetTempFileName();
                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    await model.Image.CopyToAsync(fs);
                }
                var fileName = Path.GetFileName(model.Image.FileName);
                var tmpPath = Path.GetDirectoryName(tempFile);
                var imagePath = Path.Combine(tmpPath, string.Concat(customerid, "_", fileName));
                System.IO.File.Move(tempFile, imagePath);//rename temp file
                var imageUrl = await storageHelper.UploadCustomerImage("imagecontainer", imagePath);

                //Save Customer data to Azure table
                Customer customer = new Customer(customerid.ToString(), model.InsuranceType);

                customer.FullName = model.FullName;
                customer.Email = model.Email;
                customer.Amount = model.Amount;
                customer.AppDate = model.AppDate;
                customer.EndDate = model.EndDate;
                customer.Premium = model.Premium;
                customer.ImageUrl = imageUrl;

                await storageHelper.InsertCustomerAsync("Customers", customer);

                //Add a confirmation message to Azure Queue

                await storageHelper.AddMessageAsync("insurance-requests", customer);

                return RedirectToAction("Index");





                //Add a confirmation message to Azure Queue
            }
            else
            {
                return View();
            }
        }
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
