using Newtonsoft.Json;

namespace MyTikTokBackup.Core.TikTok
{
    public class Poi
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("cityCode")]
        public string CityCode { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("fatherPoiName")]
        public string FatherPoiName { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("province")]
        public string Province { get; set; }

        [JsonProperty("ttTypeCode")]
        public string TtTypeCode { get; set; }

        [JsonProperty("ttTypeNameMedium")]
        public string TtTypeNameMedium { get; set; }

        [JsonProperty("ttTypeNameSuper")]
        public string TtTypeNameSuper { get; set; }

        [JsonProperty("ttTypeNameTiny")]
        public string TtTypeNameTiny { get; set; }

        [JsonProperty("typeCode")]
        public string TypeCode { get; set; }
    }
}
