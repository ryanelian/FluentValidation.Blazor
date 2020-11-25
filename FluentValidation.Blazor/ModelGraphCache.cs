using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Accelist.FluentValidation.Blazor
{
    /// <summary>
    /// Contains method for translating FluentValidation error property path string into a FieldIdentifier-compatible constructor values.
    /// </summary>
    internal class ModelGraphCache
    {
        /// <summary>
        /// Gets or sets the cached path to object mapping.
        /// </summary>
        private Dictionary<string, object> Cache { set; get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the root model object.
        /// </summary>
        public object Model { get; }

        /// <summary>
        /// Constructs an instance of <see cref="ModelGraphCache"/> for a model object.
        /// </summary>
        /// <param name="model"></param>
        public ModelGraphCache(object model)
        {
            this.Model = model;
        }

        /// <summary>
        /// Get object property value by string path separated by dot, supports array (IList) syntax.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="propertyPath"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public (object propertyValue, string propertyName) EvalObjectProperty(string propertyPath)
        {
            if (propertyPath.Contains(".") == false)
            {
                return (Model, propertyPath);
            }

            // FluentValidation Error PropertyName can be something like "ObjectA.ObjectB.PropertyX"
            // However, Blazor does NOT recognize nested FieldIdentifier.
            // Instead, the FieldIdentifier is assigned to the object in question. (Model + Property Name)
            // Therefore, we need to traverse the object graph to acquire them!
            var walker = Model;
            var modelObjectPath = "";
            var objectParts = propertyPath.Split('.');
            var fieldName = objectParts[objectParts.Length - 1];
            for (var i = 0; i < objectParts.Length - 1; i++)
            {
                var propertyName = objectParts[i];
                bool isArray = false;
                int arrayIndex = 0;
                if (propertyName.Contains("[") && propertyName.Contains("]"))
                {
                    // propertyName = "A[22]" --> ["A", "22"]
                    var indexedPropertyName = propertyName.Split('[', ']');
                    propertyName = indexedPropertyName[0];
                    isArray = true;
                    arrayIndex = int.Parse(indexedPropertyName[1]);
                }

                // Constructing model object path here allows capturing the same array objects without the index!
                if (string.IsNullOrEmpty(modelObjectPath))
                {
                    modelObjectPath = propertyName;
                }
                else
                {
                    modelObjectPath += "." + propertyName;
                }

                // Locally cache objects found along the way to prevent slow multiple reflection method calls
                // For Example: large array of 1000 elements will only use reflection on that array object once!
                if (Cache.ContainsKey(modelObjectPath))
                {
                    walker = Cache[modelObjectPath];
                }
                else
                {
                    walker = walker.GetType().GetProperty(propertyName)?.GetValue(walker);
                    Cache[modelObjectPath] = walker;
                }

                // System.Array implements IList https://docs.microsoft.com/en-us/dotnet/api/system.array?view=netcore-3.0
                if (isArray && walker is IList array)
                {
                    modelObjectPath += $"{modelObjectPath}[{arrayIndex}]";
                    walker = array[arrayIndex];
                }

                if (walker == null)
                {
                    break;
                }
            }

            return (walker, fieldName);
        }
    }
}
