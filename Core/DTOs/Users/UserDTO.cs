﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Users
{
    public class UserDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }

    }
}
