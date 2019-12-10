using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FoodOn.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using static FoodOn.Models.SearchViewModel;

namespace FoodOn.Controllers
{
    public class FoodController : Controller
    {
        private IGraph _graph;
        private SparqlQueryParser _parser;

        public FoodController()
        {
            _graph = new Graph();
            var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "food.owl");
            FileLoader.Load(_graph, file);

            _parser = new SparqlQueryParser();
        }

        private SparqlParameterizedString AddPrefix()
        {   SparqlParameterizedString query = new SparqlParameterizedString();
            query.Namespaces.AddNamespace("rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#"));
            query.Namespaces.AddNamespace("rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
            query.Namespaces.AddNamespace("food", new Uri("http://purl.org/heals/food/"));
            query.Namespaces.AddNamespace("ingredient", new Uri("http://purl.org/heals/ingredient/"));
            return query;
        }

        private List<string> GetAllIngredients()
        {
            List<string> ingreList = new List<string>();

            SparqlParameterizedString query = AddPrefix();
            query.CommandText = "SELECT ?ingre WHERE {?ingre rdf:type food:Ingredient.}";
            SparqlQuery queryEx = _parser.ParseFromString(query);
            Object result = _graph.ExecuteQuery(queryEx);
            if (result is SparqlResultSet)
            {
                String ingreLabel;
                SparqlResultSet rset = (SparqlResultSet)result;
                foreach (SparqlResult r in rset)
                {
                    INode n;
                    if (r.TryGetValue("ingre", out n))
                    {
                        ingreLabel = ((IUriNode)n).Uri.Segments[3];
                        ingreList.Add(ingreLabel);
                    }
                }
            }
            return ingreList;
        }

        private List<string> FindRecipes(List<string> ingreList, List<string> notIngreList,int MealType, int CourseType)
        {
            List<string> recipeList = new List<string>();

            SparqlParameterizedString query = AddPrefix();
            query.CommandText = "SELECT DISTINCT ?recipe WHERE { ?recipe rdf:type food:Recipe.";

            // Ingredient contain Query
            foreach(var item in ingreList)
            {
                query.CommandText += "?recipe food:hasIngredient ingredient:" + item + ".";
            }

            //// Ingredient NOT contain Query
            //if (notIngreList.Count > 0)
            //{
            //    query.CommandText += "FILTER ( NOT EXISTS {";
            //    foreach (var item in notIngreList)
            //    {
            //        query.CommandText += "?recipe food:hasIngredient ingredient:" + item + ".";
            //    }
            //    query.CommandText += "} )";
            //}
            
            // Meal Type Query
            if (MealType != 0)
            {
                string type = "";
                switch (MealType)
                {
                    case (int)Meal.Breakfast: type = "Breakfast"; break;
                    case (int)Meal.Lunch: type = "Lunch"; break;
                    case (int)Meal.Dinner: type = "Dinner"; break;
                    default:
                        break;
                }
                query.CommandText += "?recipe food:isRecommendedForMeal food:" + type + ".";
            }

            // Course Type Query
            if (CourseType != 0)
            {
                if (CourseType != (int)Course.LowSugar) 
                {
                    string type = "";
                    switch (CourseType)
                    {
                        case (int)Course.Dessert: type = "Dessert"; break;
                        case (int)Course.Salad: type = "Salad"; break;
                        case (int)Course.Soup: type = "Soup"; break;
                        default:
                            break;
                    }
                    query.CommandText += "?recipe food:isRecommendedForCourse food:" + type + ".";
                }
                //else // Low Sugar Query
                //{
                //    query.CommandText += "?recipe food:hasIngredient ?ingredient. ?ingredient food:hasGlycemicIndex ?GI. FILTER (?GI <= 55)";
                //}
            }
            query.CommandText += "}";

            SparqlQuery queryEx = _parser.ParseFromString(query);
            Object result = _graph.ExecuteQuery(queryEx);
            if (result is SparqlResultSet)
            {
                String recipeLabel;
                SparqlResultSet rset = (SparqlResultSet)result;
                foreach (SparqlResult r in rset)
                {
                    INode n;
                    if (r.TryGetValue("recipe", out n))
                    {
                        recipeLabel = ((IUriNode)n).Uri.Segments[3];
                        recipeList.Add(recipeLabel);
                    }
                }
            }
            if (recipeList.Count == 0) recipeList.Add("No result!");
            return recipeList;
        }

        private RecipeViewModel GetRecipeByName(string name)
        {
            RecipeViewModel recipe = new RecipeViewModel();
            recipe.Name = name;
            SparqlParameterizedString query = AddPrefix();

            // Ingredients List
            query.CommandText = "SELECT ?ingre WHERE {?recipe food:hasIngredient ?ingre. FILTER( ?recipe = ingredient:" + name +")}";
            SparqlQuery queryEx = _parser.ParseFromString(query);
            Object result = _graph.ExecuteQuery(queryEx);
            if (result is SparqlResultSet)
            {
                String ingreLabel;
                SparqlResultSet rset = (SparqlResultSet)result;
                foreach (SparqlResult r in rset)
                {
                    INode n;
                    if (r.TryGetValue("ingre", out n))
                    {
                        ingreLabel = ((IUriNode)n).Uri.Segments[3];
                        recipe.Ingredients.Add(ingreLabel);
                    }
                }
            }

            // Cooking Time, Meal Type, Course Type
            query.CommandText = "SELECT ?time ?meal ?course WHERE {?recipe food:hasCookTime ?time. " +
                "?recipe food:isRecommendedForMeal ?meal. " +
                "?recipe food:isRecommendedForCourse ?course. " +
                "FILTER( ?recipe = ingredient:" + name +")}";

            queryEx = _parser.ParseFromString(query);
            result = _graph.ExecuteQuery(queryEx);
            if (result is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)result;
                foreach (SparqlResult r in rset)
                {
                    INode n;
                    if (r.TryGetValue("time", out n))
                    {
                        recipe.CookingTime = ((ILiteralNode)n).Value;
                    }
                    if (r.TryGetValue("meal", out n))
                    {
                        recipe.MealType = ((IUriNode)n).Uri.Segments[3];
                    }
                    if (r.TryGetValue("course", out n))
                    {
                        recipe.CourseType = ((IUriNode)n).Uri.Segments[3];
                    }
                }
            }

            // Glycemic
            query.CommandText = "SELECT (SUM(?GI) as ?glycemic) " +
                "WHERE{?recipe food:hasIngredient ?ingredient. " +
                "?ingredient food:hasGlycemicIndex ?GI." +
                "FILTER( ?recipe = ingredient:"+ name +")}";

            queryEx = _parser.ParseFromString(query);
            result = _graph.ExecuteQuery(queryEx);
            if (result is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)result;
                foreach (SparqlResult r in rset)
                {
                    INode n;
                    if (r.TryGetValue("glycemic", out n))
                    {
                        recipe.Glycemic = ((ILiteralNode)n).Value;
                    }
                }
            }

            return recipe;
        }
        public IActionResult Index()
        {
            var ingreList = GetAllIngredients();
            ViewBag.Ingredients = new SelectList(ingreList);
            return View(new SearchViewModel());
        }

        [HttpPost]
        public IActionResult Index(SearchViewModel model)
        {
            var result = FindRecipes(model.Ingredients, model.NotIngredients, model.MealType, model.CourseType);
            return PartialView("_Result", result);
        }

        public IActionResult Recipe(string name)
        {
            var recipe = GetRecipeByName(name);
            return PartialView("_Info", recipe);
        }
    }
}