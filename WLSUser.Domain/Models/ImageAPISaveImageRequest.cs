using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WLSUser.Domain.Models
{
    public class ImageAPISaveImageRequest
    {
        [Required]
        public string ImageType { get; set; }

        [Required]
        public string ImageData { get; set; }
    }
}
