//using System.Web.Http;
//using System.Web.Routing;
//using Microsoft.AspNet.OData.Builder;
//using Microsoft.AspNet.OData.Extensions;
//using Microsoft.OData.Edm;
//using XCode.Membership;

//namespace NewLife.Cube
//{
//    public class ODataConfig
//    {
//        /// <summary>
//        /// OData注册
//        /// </summary>
//        /// <param name="config"></param>
//        public static void Register(HttpConfiguration config)
//        {
//            var oDataRoute = config.MapODataServiceRoute(
//                routeName: "ODataRoute",
//                routePrefix: "odata",
//                model: GetEdmModel());
//        }

//        static IEdmModel GetEdmModel()
//        {
//            var builder = new ODataConventionModelBuilder();
//            builder.EntitySet<UserX>("UserXs");
//            return builder.GetEdmModel();
//        }
//    }
//}