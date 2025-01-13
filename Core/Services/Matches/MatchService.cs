using Core.DTOs.Matches;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Matches
{
    public class MatchService : IMatchService
    {
        private readonly IRepositoryManager _repository;
        public MatchService(IRepositoryManager repository)
        {
            _repository = repository;
        }
        public async Task AddNewMatch(AddMatchDetailsDto addMatchDetailsDto)
        {
            var teamOne = await _repository.Team.GetByIdAsync(addMatchDetailsDto.TeamOneId);
            if (teamOne == null)
                throw new ArgumentException($"Team with ID {addMatchDetailsDto.TeamOneId} does not exist.");

            var teamTwo = await _repository.Team.GetByIdAsync(addMatchDetailsDto.TeamTwoId);
            if (teamTwo == null)
                throw new ArgumentException($"Team with ID {addMatchDetailsDto.TeamTwoId} does not exist.");

            var stadium = await _repository.Stadium.GetByIdAsync(addMatchDetailsDto.StadiumId);
            if (stadium == null)
                throw new ArgumentException($"Stadium with ID {addMatchDetailsDto.StadiumId} does not exist.");

            var match = new Match
            {
                Date = addMatchDetailsDto.TimeOfMatch,
                StadiumId = addMatchDetailsDto.StadiumId,
                Stadium = stadium,
                TeamOneId = addMatchDetailsDto.TeamOneId,
                TeamOne = teamOne,
                TeamTwoId = addMatchDetailsDto.TeamTwoId,
                TeamTwo = teamTwo
            };

            _repository.Match.Create(match);
            await _repository.SaveAsync();
        }

        public List<MatchDTO> GetMatchList()
        {
            var matchList = _repository.Match.GetAllAsync();
            var matchListDto = matchList.Select(e => new MatchDTO
            {
                StadiumName = e.StadiumId,
                TeamOneId = e.TeamOneId,
                TeamTwoId = e.TeamTwoId,
                TimeOfMatch = e.Date

            }).ToList();
            return matchListDto;
        }
    }
}
