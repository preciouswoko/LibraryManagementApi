using LibraryManagement.Data.Response;
using LibraryManagement.Data.ViewModel;
using LibraryManagement.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BorrowingController : ControllerBase
    {
        private readonly IBorrowingService _service;
        public BorrowingController(IBorrowingService service)
        {
            _service = service;
        }

        // POST: BorrowingController /Create
        [Route("BorrowBook")]
        [HttpPost]
        public async Task<IActionResult> BorrowBook([FromBody] BorrowingRequest request)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                    return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "PLease Add the required value", Data = null }));

                }
               var result = await _service.IssueBook(request);
                return Ok(new StandardResponse { ResponseCode = "00", ResponseMessage = "Sucessfull", Data = result });
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        // PUT: BorrowingController/Edit
        [Route("ReturnBook")]
        [HttpPut]
        public async Task<IActionResult> ReturnBook( string bookname, [FromBody] DateTime returndate)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var res = await _service.ReturnBook(bookname, returndate);
                    if (res == 1)
                    {
                        return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "Book Not Found", Data = null }));
                    }
                    return Ok(new StandardResponse { ResponseCode = "00", ResponseMessage = $"Sucessfully return book and you owe NGN{res}" });
                }
                return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "invalid details", Data = null }));

            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }
    }
}
