﻿// ====================================================================================================================
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

using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Linq;

namespace Cocktail
{
    /// <summary>
    /// An implementation of <see cref="ICompositionProvider"/> which uses MEF as the underlying IoC implementation.
    /// </summary>
    internal partial class MefCompositionProvider : ICompositionProvider
    {
        private ConventionBuilder _conventions;
        private CompositionHost _container;
        private ContainerConfiguration _configuration;

        public ConventionBuilder Conventions
        {
            get { return _conventions ?? (_conventions = new ConventionBuilder()); }
        }
        
        public ContainerConfiguration Configuration
        {
            get
            {
                if (_configuration != null)
                    return _configuration;

                Conventions
                    .ForTypesDerivedFrom<IValidationErrorNotification>()
                    .Export<IValidationErrorNotification>();
                Conventions
                    .ForTypesDerivedFrom<IDiscoverableViewModel>()
                    .Export<IDiscoverableViewModel>();
                Conventions
                    .ForTypesDerivedFrom<IConnectionOptionsResolver>()
                    .Export<IConnectionOptionsResolver>();
                Conventions
                    .ForTypesDerivedFrom<IEntityManagerSyncInterceptor>()
                    .Export<IEntityManagerSyncInterceptor>();
                Conventions
                    .ForTypesDerivedFrom<EntityManagerDelegate>()
                    .Export<EntityManagerDelegate>();
                Conventions
                    .ForType<EventAggregator>()
                    .Export<IEventAggregator>()
                    .Shared();

                var assemblies = IdeaBlade.Core.Composition.CompositionHost.Instance.ProbeAssemblies;

                return _configuration = new ContainerConfiguration()
                    .WithAssemblies(assemblies, Conventions);
            }
        }

        public CompositionHost Container
        {
            get { return _container ?? (_container = Configuration.CreateContainer()); }
        }

        public void Configure(ConventionBuilder conventions)
        {
            _conventions = conventions;
        }

        public Lazy<T> GetInstance<T>() where T : class
        {
            return new Lazy<T>(() => Container.GetExport<T>());
        }

        public IEnumerable<T> GetInstances<T>() where T : class
        {
            return Container.GetExports<T>();
        }

        public Lazy<object> GetInstance(Type serviceType, string contractName)
        {
             return new Lazy<object>(() => Container.GetExport(serviceType, contractName));
        }

        public IEnumerable<object> GetInstances(Type serviceType, string contractName)
        {
            return Container.GetExports(serviceType, contractName);
        }

        public ICompositionFactory<T> GetInstanceFactory<T>() where T : class
        {
            var factory = new MefCompositionFactory<T>();
            Container.SatisfyImports(factory);
            return factory;
        }

        public void BuildUp(object instance)
        {
            // Skip if in design mode.
            if (Execute.InDesignMode)
                return;

            Container.SatisfyImports(instance);
        }

        public T TryGetInstance<T>() where T : class
        {
            T instance;
            if (!Container.TryGetExport<T>(out instance))
                return null;

            return instance;
        }

        public object TryGetInstance(Type serviceType, string contractName)
        {
            object instance;
            if (!Container.TryGetExport(serviceType, contractName, out instance))
                return null;

            return instance;
        }

        public ICompositionFactory<T> TryGetInstanceFactory<T>() where T : class
        {
            var factory = new MefCompositionFactory<T>();
            Container.SatisfyImports(factory);
            if (factory.ExportFactory == null)
                return null;

            return factory;
        }
    }
}
