// Controllers/TeeTimesController.cs
using Microsoft.AspNetCore.Mvc;
using GolfBAIST.TechnicalServices;
using System;

namespace GolfBAIST.Controllers
{
    [Route("api/teetimes")]
    [ApiController]
    public class TeeTimesController : ControllerBase
    {
        private readonly TeeTimesService _teeTimesService;

        public TeeTimesController(TeeTimesService teeTimesService)
        {
            _teeTimesService = teeTimesService;
        }
       
        [HttpGet("available")]
        public IActionResult GetAvailableTimeSlots([FromQuery] string date)
        {
            if (!DateTime.TryParse(date, out DateTime parsedDate))
                return BadRequest("Invalid date format.");

            var openSlots = _teeTimesService.GetAvailableTimeSlots(parsedDate);

            return Ok(openSlots); // Return clean JSON
        }


    }
}
