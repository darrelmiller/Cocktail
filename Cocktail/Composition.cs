// ====================================================================================================================
//   Copyright (c) 2012 IdeaBlade
// ====================================================================================================================
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//   WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS 
//   OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
//   OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
// ====================================================================================================================
//   USE OF THIS SOFTWARE IS GOVERENED BY THE LICENSING TERMS WHICH CAN BE FOUND AT
//   http://cocktail.ideablade.com/licensing
// ====================================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using IdeaBlade.Core.Composition;
using CompositionHost = IdeaBlade.Core.Composition.CompositionHost;

namespace Cocktail
{
    /// <summary>
    ///   Sets up a composition container and provides means to interact with the container.
    /// </summary>
    public static partial class Composition
    {
        /// <summary>
        ///   Returns the current catalog in use.
        /// </summary>
        /// <returns> Unless a custom catalog is provided through <see cref="Configure" />, this property returns <see cref="DefaultCatalog" /> </returns>
        public static ComposablePartCatalog Catalog
        {
            get { return _catalog ?? DefaultCatalog; }
        }

        /// <summary>
        ///   Returns the default catalog in use by DevForce.
        /// </summary>
        public static ComposablePartCatalog DefaultCatalog
        {
            get { return CompositionHost.Instance.Container.Catalog; }
        }

        /// <summary>
        ///   Returns the CompositionContainer in use.
        /// </summary>
        public static CompositionContainer Container
        {
            get { return _container ?? (_container = new CompositionContainer(Catalog)); }
        }

        internal static bool IsRecomposing { get; set; }

        /// <summary>
        ///   Executes composition on the container, including the changes in the specified <see cref="CompositionBatch" /> .
        /// </summary>
        /// <param name="compositionBatch"> Changes to the <see cref="CompositionContainer" /> to include during the composition. </param>
        public static void Compose(CompositionBatch compositionBatch)
        {
            if (compositionBatch == null)
                throw new ArgumentNullException("compositionBatch");

            Container.Compose(compositionBatch);
        }

        /// <summary>
        ///   Resets the CompositionContainer to it's initial state.
        /// </summary>
        /// <remarks>
        ///   After calling <see cref="Clear" /> , <see cref="Configure" /> should be called to configure the new CompositionContainer.
        /// </remarks>
        public static void Clear()
        {
            if (_container != null)
                _container.Dispose();
            _container = null;
            _catalog = null;

            Cleared(null, EventArgs.Empty);
        }

        /// <summary>
        ///   <para>Returns an instance of the custom implementation for the provided type.</para>
        /// </summary>
        /// <typeparam name="T"> Type of the requested instance. </typeparam>
        /// <param name="requiredCreationPolicy"> Optionally specify whether the returned instance should be a shared, non-shared or any instance. </param>
        /// <returns> The requested instance. </returns>
        public static T GetInstance<T>(CreationPolicy requiredCreationPolicy = CreationPolicy.Any)
        {
            var exports = GetExportsCore(typeof(T), null, requiredCreationPolicy).ToList();
            if (!exports.Any())
                throw new Exception(string.Format(StringResources.CouldNotLocateAnyInstancesOfContract,
                                                  typeof(T).FullName));

            return exports.Select(e => e.Value).Cast<T>().First();
        }

        /// <summary>
        ///   <para>Returns all instances of the custom implementation for the provided type.</para>
        /// </summary>
        /// <typeparam name="T"> Type of the requested instances. </typeparam>
        /// <param name="requiredCreationPolicy"> Optionally specify whether the returned instances should be shared, non-shared or any instances. </param>
        /// <returns> The requested instances. </returns>
        public static IEnumerable<T> GetInstances<T>(CreationPolicy requiredCreationPolicy = CreationPolicy.Any)
        {
            var exports = GetExportsCore(typeof(T), null, requiredCreationPolicy);
            return exports.Select(e => e.Value).Cast<T>();
        }

        /// <summary>
        ///   <para>Returns an instance of the custom implementation for the provided type or contract name.</para>
        /// </summary>
        /// <param name="serviceType"> The type of the requested instance. </param>
        /// <param name="key"> The contract name of the instance requested. If no contract name is specified, the type will be used. </param>
        /// <param name="requiredCreationPolicy"> Optionally specify whether the returned instance should be a shared, non-shared or any instance. </param>
        /// <returns> The requested instance. </returns>
        public static object GetInstance(Type serviceType, string key,
                                         CreationPolicy requiredCreationPolicy = CreationPolicy.Any)
        {
            var exports = GetExportsCore(serviceType, key, requiredCreationPolicy).ToList();
            if (!exports.Any())
                throw new Exception(string.Format(StringResources.CouldNotLocateAnyInstancesOfContract,
                                                  serviceType != null ? serviceType.ToString() : key));

            return exports.First().Value;
        }

        /// <summary>
        ///   <para>Returns all instances of the custom implementation for the provided type.</para>
        /// </summary>
        /// <param name="serviceType"> Type of the requested instances. </param>
        /// <param name="requiredCreationPolicy"> Optionally specify whether the returned instances should be shared, non-shared or any instances. </param>
        /// <returns> The requested instances. </returns>
        public static IEnumerable<object> GetInstances(Type serviceType,
                                                       CreationPolicy requiredCreationPolicy = CreationPolicy.Any)
        {
            var exports = GetExportsCore(serviceType, null, requiredCreationPolicy);
            return exports.Select(e => e.Value);
        }

        /// <summary>
        ///   Fired when the composition container is modified after initialization.
        /// </summary>
        public static event EventHandler<RecomposedEventArgs> Recomposed
        {
            add { CompositionHost.Recomposed += value; }
            remove { CompositionHost.Recomposed -= value; }
        }

        /// <summary>
        ///   Fired after <see cref="Clear" /> has been called to clear the current CompositionContainer.
        /// </summary>
        public static event EventHandler<EventArgs> Cleared = delegate { };

        internal static IEnumerable<Export> GetExportsCore(Type serviceType, string key, CreationPolicy policy)
        {
            var contractName = string.IsNullOrEmpty(key) ? AttributedModelServices.GetContractName(serviceType) : key;
            var requiredTypeIdentity = serviceType != null
                                           ? AttributedModelServices.GetTypeIdentity(serviceType)
                                           : null;
            var importDef = new ContractBasedImportDefinition(
                contractName,
                requiredTypeIdentity,
                Enumerable.Empty<KeyValuePair<string, Type>>(),
                ImportCardinality.ZeroOrMore,
                false,
                true,
                policy);

            return Container.GetExports(importDef);
        }

        internal static bool ExportExists<T>()
        {
            return Container.GetExports<T>().Any();
        }
    }
}