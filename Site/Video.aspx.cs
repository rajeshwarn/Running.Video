using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Video : System.Web.UI.Page
{
  protected void Page_Load(object sender, EventArgs e)
  {
    //dG.DataSource =
    //  GerenciadorDeProcessamentoDeVideo.TodosProcessamentos;
    //dG.DataBind();
  }

  protected void Unnamed2_Click(object sender, EventArgs e)
  {
    //Regex notLetterOrNumber = new Regex(@"^\w|");
    if (fUp.HasFiles || fUp.HasFile)
    {
      foreach (var file in fUp.PostedFiles) {
        var newName = Regex.Replace(file.FileName, @"[^a-zA-Z0-9\.\-_]+?", "_");
        file.SaveAs(
          Path.Combine(Server.MapPath("~/_video_/in"), newName)
        );
        //GerenciadorDeProcessamentoDeVideo.ColocarNaFila(newName);
      }
    }
    //dG.DataSource =
    //  GerenciadorDeProcessamentoDeVideo.TodosProcessamentos;
    //dG.DataBind();


  }
}