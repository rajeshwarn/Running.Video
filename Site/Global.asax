<%@ Application Language="C#" %>
<%@ Import Namespace="Site" %>
<%@ Import Namespace="System.Web.Optimization" %>
<%@ Import Namespace="System.Web.Routing" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e)
    {
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
        GerenciadorDeProcessamentoDeVideo.Inicializar();
    }

</script>
