using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing;

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
            query.Namespaces.AddNamespace("ingredient", new Uri("http://purl.org/heals/ingredient#"));
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
        public IActionResult Index()
        {
            var ingreList = GetAllIngredients();
            ViewBag.Ingredients = new SelectList(ingreList);
            return View();
        }
        
        [HttpPost]
        public IActionResult Index(string[] ingredientList)
        {
            var ingreList = ingredientList;
            return View();
        }
    }
}