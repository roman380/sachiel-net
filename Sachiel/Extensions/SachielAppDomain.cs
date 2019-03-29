using System;
using System.Diagnostics;
using System.Collections.Generic;
#if !NET_45
using Microsoft.Extensions.DependencyModel;
#endif
using System.Linq;
using System.Reflection;

namespace Sachiel.Extensions
{
    public class SachielAppDomain
    {
        public static SachielAppDomain CurrentDomain { get; }

        static SachielAppDomain()
        {
            CurrentDomain = new SachielAppDomain();
        }

        public class LoadContext
        {
            public Dictionary<string, bool> AssemblyLoadState = new Dictionary<string, bool>();
        }

        /// <summary>
        /// A replacement for the AppDomain.GetAssemblies function.
        /// </summary>
        /// <returns></returns>
        public Assembly[] GetAssemblies(LoadContext context)
        {
            Assembly[] ass = null;
            #if NET_CORE
            ass =  GetNetCoreAssemblies();
            #else
            ass = GetFrameAssemblies(context);
            #endif
          
            return ass;
        }

        private Assembly[] GetFrameAssemblies(LoadContext context)
        {
            return GetReferencingAssemblies(context, Assembly.GetEntryAssembly());
        }

        private Assembly[] GetNetCoreAssemblies()
        {
            var dependencies = DependencyContext.Default.RuntimeLibraries;
            return (from library in dependencies where IsCandidateCompilationLibrary(library) select Assembly.Load(new AssemblyName(library.Name))).ToArray();
        }

        private static Assembly[] GetReferencingAssemblies(LoadContext context, Assembly assembly)
        {
            var assemblies = new List<Assembly> {assembly};
            foreach (var library in assembly.GetReferencedAssemblies())
            {
                try
                {
                    if (context.AssemblyLoadState.ContainsKey(library.FullName))
                        continue;
                    assemblies.Add(Assembly.Load(new AssemblyName(library.FullName)));
                    //Trace.WriteLine($"Assembly.Load succeeded, library.FullName {library.FullName}");
                    context.AssemblyLoadState.Add(library.FullName, true);
                }
                catch (Exception e)
                {
                    // NOTE: Esp. FileLoadException "A file that was found could not be loaded."?
                    Trace.WriteLine($"Assembly.Load failed, library.FullName {library.FullName}, {e.Message} ({e.GetType().FullName})");
                    context.AssemblyLoadState.Add(library.FullName, false);
                    // ignored
                }
            }
            return assemblies.Distinct().ToArray();
        }

        private static bool IsCandidateCompilationLibrary(RuntimeLibrary compilationLibrary)
        {
            return compilationLibrary.Name == ("Specify")
                   || compilationLibrary.Dependencies.Any(d => d.Name.StartsWith("Specify"));
        }
    }
}
