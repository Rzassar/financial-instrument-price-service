using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models;

namespace Amega.UI.WebApi.Model.AmegaSettings
{
    public sealed class AssetClassSettings
    {
        public eAssetClassType AssetClassType { get; set; }

        public string Url { get; set; }

        /// <summary>
        /// Accommodates extra properties in JSON formatted.
        /// </summary>
        public string Extended { get; set; }
    }
}