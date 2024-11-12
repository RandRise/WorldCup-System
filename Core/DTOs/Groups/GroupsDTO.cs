namespace Core.DTOs.Groups
{
    public class GroupsDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int WorldCupId { get; set; }
    }
    public class AddGroupsDTO
    {
        public string Name { get; set; }
        public int WorldCupId { get; set; }

    }
}
