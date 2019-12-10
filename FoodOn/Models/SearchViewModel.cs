using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodOn.Models
{
    public class SearchViewModel
    {
        public enum Meal { All, Breakfast, Lunch, Dinner}
        public enum Course { All, Dessert, Salad, Soup, LowSugar}
        public List<string> Ingredients { get; set; } = new List<string>();
        public List<string> NotIngredients { get; set; } = new List<string>();
        public int MealType { get; set; } = (int)Meal.All;
        public int CourseType { get; set; } = (int)Course.All;

    }
}
