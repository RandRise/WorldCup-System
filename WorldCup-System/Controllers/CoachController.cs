using Core.DTOs.Coaches;
using Core.Services.Coaches;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CoachController : Controller
    {
        private readonly ICoachService _coachService;
        public CoachController(ICoachService coachService)
        {
            _coachService = coachService;
        }
        [HttpGet]
        public List<CoachDTO> GetCoachList()
        {
            var coachList = _coachService.GetCoachList();
            return coachList;
        }

        [HttpPost]
        public async Task<IActionResult> AddCoach([FromBody] AddCoachDto addCoachDTO)
        {
            try
            {
                await _coachService.AddNewCoach(addCoachDTO);
                return Ok("Coach Added Successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
