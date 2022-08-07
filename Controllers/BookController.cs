using LibraryManagement.Data.Response;
using LibraryManagement.Data.ViewModel;
using LibraryManagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LibraryManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly IBookService _bookService;
        public BookController(IBookService bookService)
        {
            _bookService = bookService;
        }
        // GET: BookController
        [Route("GetBooks")]
        [HttpGet]
        public async Task<IActionResult> GetBooks()
        {
            try
            {
                var val = _bookService.GetBooks();
                if (val.Count == 0)
                {
                    return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "No Book to Display", Data = null }));
                }
                return Ok(new StandardResponse { ResponseCode = "00", ResponseMessage = "Sucessfull", Data = val });
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }

        // POST: BookController /Create
        [Route("AddBooks")]
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddBooks([FromBody] BookRequest request)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                    return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "PLease Add the required value", Data = null }));

                }
                var result = await _bookService.AddBook(request);
                return Ok(new StandardResponse { ResponseCode = "00", ResponseMessage = "Sucessfull", Data = result });
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        // GET: BookController/Id
        [Route("GetBookById")]
        [HttpGet]
        public async Task<IActionResult> GetBookById(int bookid)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var val = await _bookService.GetByBookId(bookid);
                    if (val.Count == 0)
                    {
                        return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "Book Not Found", Data = null }));
                    }
                    return Ok(new StandardResponse { ResponseCode = "00", ResponseMessage = "Sucessfull", Data = val });
                }
                return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "Id Not Found", Data = null }));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }


        }


        // PUT: BookController/Edit
        [Route("UpdateBook")]
        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBook([FromBody] BookRequest request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool res = await _bookService.UpdateBook(request);
                    if (res == false)
                    {
                        return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "Book Not Found", Data = null }));
                    }
                    return Ok(new StandardResponse { ResponseCode = "00", ResponseMessage = "Sucessfull", Data = res });
                }
                return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "invalid details", Data = null }));

            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }

        // DELETE BookController/Delete
        [Route("DeleteBook")]
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                bool res = await _bookService.DeleteBook(id);
                if (res == false)
                {
                    return (StatusCode(400, new StandardResponse { ResponseCode = "99", ResponseMessage = "Id Not Found", Data = null }));
                }
                return Ok(new StandardResponse { ResponseCode = "00", ResponseMessage = "Sucessfull", Data = res });
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }
    }
}
