using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodOn.Models
{
    public class RecipeViewModel
    {
        public string Name { get; set; }
        public List<string> Ingredients { get; set; } = new List<string>();
        public string CookingTime { get; set; } = "0";
        public string MealType { get; set; } = "None";
        public string CourseType { get; set; } = "None";
        public string Description { get; set; } = "None";
        public string Glycemic { get; set; } = "None";
    }
}
