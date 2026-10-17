using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TestWebAPI.Domain.Enum
{
    public enum enmRole
    {
        unkown=0,
        [Display(Name = "User")]
        user =1

    }
}
