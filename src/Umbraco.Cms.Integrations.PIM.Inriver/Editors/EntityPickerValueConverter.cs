﻿using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

using Umbraco.Cms.Integrations.PIM.Inriver;
using Umbraco.Cms.Integrations.PIM.Inriver.Models;
using Umbraco.Cms.Integrations.PIM.Inriver.Models.ViewModels;
using Umbraco.Cms.Integrations.PIM.Inriver.Services;

namespace Umbraco.Cms.Integrations.Pim.Inriver.Editors
{
    public class EntityPickerValueConverter : PropertyValueConverterBase
    {
        private readonly IInriverService _inriverService;

        public EntityPickerValueConverter(IInriverService inriverService)
        {
            _inriverService = inriverService;   
        }

        public override bool IsConverter(IPublishedPropertyType propertyType) => propertyType.EditorAlias.Equals(Constants.PropertyEditorAlias);

        public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) => PropertyCacheLevel.Snapshot;

        public override Type GetPropertyValueType(IPublishedPropertyType propertyType) => typeof(InriverEntityViewModel);

        public override object ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType propertyType, object source, bool preview)
        {
            if (source == null)
            {
                return null;
            }

            var node = JsonNode.Parse(source.ToString());

            int entityId = (int) node["entityId"];

            var displayFields = node["displayFields"] as JsonArray;

            var fetchDataResponse = _inriverService.FetchData(new FetchDataRequest
            {
                EntityIds = new[] { entityId },
                FieldTypeIds = string.Join(",", displayFields.Select(p => p["fieldTypeId"].GetValue<string>()))
            }).ConfigureAwait(false).GetAwaiter().GetResult();

            if (fetchDataResponse.Failure) return null;

            var entityData = fetchDataResponse.Data.FirstOrDefault();
            if (entityData == null) return null;

            var vm = new InriverEntityViewModel
            {
                DisplayName = entityData.Summary.DisplayName,
                DisplayDescription = entityData.Summary.Description,
                ResourceUrl = entityData.Summary.ResourceUrl,
                Fields = entityData.Fields.ToDictionary(
                    x => displayFields.First(p => p["fieldTypeId"].GetValue<string>() == x.FieldTypeId)["fieldTypeDisplayName"].GetValue<string>(), 
                    x => x.Value)                
            };

            return vm;
        }
    }
}

