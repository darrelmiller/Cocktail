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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdeaBlade.Core;
using IdeaBlade.Core.Composition;
using IdeaBlade.EntityModel;

namespace Cocktail
{
    public static partial class Composition
    {
        private static readonly Dictionary<string, XapDownloadOperation> XapDownloadOperations =
            new Dictionary<string, XapDownloadOperation>();

        /// <summary>Asynchronously downloads a XAP file and adds all exported parts to the catalog.</summary>
        /// <param name="relativeUri">The relative URI for the XAP file to be downloaded.</param>
        /// <returns>The asynchronous download <see cref="Task"/>.</returns>
        public static Task AddXapAsync(string relativeUri)
        {
            XapDownloadOperation operation;
            if (XapDownloadOperations.TryGetValue(relativeUri, out operation) && !operation.Task.IsFaulted)
                return operation.Task;

            operation = XapDownloadOperations[relativeUri] = new XapDownloadOperation(relativeUri);
            return operation.Task;
        }
    }

    internal class XapDownloadOperation
    {
        private readonly DynamicXap _xap;
        private readonly TaskCompletionSource<bool> _tcs;

        public XapDownloadOperation(string xapUri)
        {
            _tcs = new TaskCompletionSource<bool>();
            _xap = new DynamicXap(new Uri(xapUri, UriKind.Relative));
            _xap.Loaded += (s, args) => XapDownloadCompleted(args);
        }

        public Task Task
        {
            get { return _tcs.Task; }
        }

        private void XapDownloadCompleted(DynamicXapLoadedEventArgs args)
        {
            if (args.Cancelled)
            {
                _tcs.SetCanceled();
                return;
            }

            if (!args.HasError)
            {
                Composition.IsRecomposing = true;
                try
                {
                    CompositionHost.Add(_xap);
                    _tcs.SetResult(true);
                }
                catch (Exception e)
                {
                    _tcs.SetException(e);
                }
                finally
                {
                    Composition.IsRecomposing = false;
                }
            }
            else
                _tcs.SetException(args.Error);
        }
    }
}