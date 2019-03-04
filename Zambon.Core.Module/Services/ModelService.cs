﻿using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Zambon.Core.Database;
using Zambon.Core.Module.ExtensionMethods;
using Zambon.Core.Module.Xml;

namespace Zambon.Core.Module.Services
{
    /// <summary>
    /// Service used to deserialize the Application Model from XML.
    /// </summary>
    public class ModelService
    {
        
        #region Variables

        private readonly IOptions<ApplicationConfigs> AppConfigs;

        private Dictionary<string, Application> Model;

        #endregion

        #region Properties

        private string _AppVersion;
        /// <summary>
        /// The current version of the application, from the startup project Package > Package version.
        /// </summary>
        public string AppVersion {
            get {
                if (string.IsNullOrWhiteSpace(_AppVersion))
                {
                    var assembly = Assembly.GetEntryAssembly();
                    _AppVersion = (assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute)).FirstOrDefault() as AssemblyInformationalVersionAttribute).InformationalVersion;
                    _AppCopyright = (assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute)).FirstOrDefault() as AssemblyCopyrightAttribute).Copyright;
                }
                return _AppVersion;
            }
        }

        private string _AppCopyright;
        /// <summary>
        /// The copyright of the application, from the startup project Package > Copyright.
        /// </summary>
        public string AppCopyright {
            get {
                if (string.IsNullOrWhiteSpace(_AppCopyright))
                {
                    var assembly = Assembly.GetEntryAssembly();
                    _AppVersion = (assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute)).FirstOrDefault() as AssemblyInformationalVersionAttribute).InformationalVersion;
                    _AppCopyright = (assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute)).FirstOrDefault() as AssemblyCopyrightAttribute).Copyright;
                }
                return _AppCopyright;

            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for the ApplicationService.
        /// </summary>
        /// <param name="appConfigs">Instance of the AppSettings.json file.</param>
        public ModelService(IOptions<ApplicationConfigs> appConfigs)
        {
            AppConfigs = appConfigs;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get a cloned instance of the Application Model.
        /// </summary>
        /// <param name="ctx">The CoreDbContext instance.</param>
        /// <param name="language">The current language of the model, if null will return the default language.</param>
        /// <returns>Return a cloned instance of the Application Model</returns>
        public Application GetModel(CoreDbContext ctx, string language)
        {
            if (Model == null)
                LoadAllModelFiles(ctx);

            Application model;
            if (string.IsNullOrWhiteSpace(language) || !Model.ContainsKey(language))
                model = (Application)Model.FirstOrDefault().Value.Clone();
            else
                model = (Application)Model[language].Clone();

            model.OnLoadingUserModel(model, ctx);
            return model;
        }

        private void LoadAllModelFiles(CoreDbContext ctx)
        {
            if (Model == null)
                Model = new Dictionary<string, Application>();
            else
                Model.Clear();

            if ((AppConfigs.Value.Languages?.Length ?? 0) == 0)
                Model.Add("", LoadModelFile(ctx));
            else
                for (var l = 0; l < AppConfigs.Value.Languages.Length; l++)
                {
                    var language = l == 0 ? "" : AppConfigs.Value.Languages[l];
                    Model.Add(language, LoadModelFile(ctx, language));
                }

            foreach (var applicationModel in Model.Values)
                applicationModel.OnLoadingXml(applicationModel, ctx);
        }

        private Application LoadModelFile(CoreDbContext ctx, string language = "")
        {
            var fileName = string.Format("ApplicationModel{0}.xml", !string.IsNullOrWhiteSpace(language) ? "." + language : "");
            var serializer = new XmlSerializer(typeof(Application));

            Application model = null;

            //Search in WebModule if exists the ApplicationModel XML base file.
            using (var webModuleStream = Assembly.Load("Zambon.Core.WebModule").GetManifestResourceStream(string.Format("Zambon.Core.WebModule.{0}", fileName)))
                if(webModuleStream != null)
                    model = (Application)serializer.Deserialize(webModuleStream);

            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), fileName);
            using (var applicationStream = File.Exists(path) ? File.Open(path, FileMode.Open, FileAccess.Read) : Assembly.GetEntryAssembly().GetManifestResourceStream($"{Assembly.GetEntryAssembly().GetName().Name}.{fileName}"))
                if (applicationStream != null)
                {
                    var applicationModel = (Application)serializer.Deserialize(applicationStream);
                    model = model != null ? model.MergeObject(applicationModel) : applicationModel; 
                }

            if(!string.IsNullOrWhiteSpace(language) && Model.Count() > 0)
            {
                //If is inserting any language and has already inserted at least one in _Model, this means this in an alternative language.
                //Must then merge with the default (First) language.
                model = ((Application)Model.FirstOrDefault().Value.Clone()).MergeObject(model);
            }

            return model;
        }


        internal ApplicationConfigs GetAppSettings()
        {
            return AppConfigs.Value;
        }
        
        #endregion

    }
}