<%@ Page Title=""
    Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="Video.aspx.cs" Inherits="Video" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="Server">
    <style>
        .opaco {
            opacity: 0;
            position: absolute;
            top: 0px;
            left: 0px;
        }
    </style>
    <div class="form-horizontal">
        <h4>Codificar arquivo</h4>
        <hr />
        <div class="form-group">
            <label class="col-md-2 control-label"></label>
            <div class="col-md-12">
                <div class="input-group ">
                    <input type="text" class="form-control" placeholder="Search for...">
                    <span class="input-group-btn">
                        <label class="btn btn-default btn-file">
                            Browse
                            <asp:FileUpload runat="server"
                                Accept=".mp4,.mkv,.avi"
                                AllowMultiple="true" ID="fUp" CssClass="opaco" />
                        </label>
                    </span>
                </div>
            </div>
        </div>
    </div>
    <div class="row">
        <div class="col-md-8">
        </div>
        <asp:Button runat="server" Text="Carregar e Processar" OnClick="Unnamed2_Click" CssClass="btn btn-default" />
    </div>






    <asp:DataGrid runat="server" ID="dG" AutoGenerateColumns="false">
        <Columns>
            <asp:BoundColumn DataField="NomeArquivo" HeaderText="Arquivo"></asp:BoundColumn>
            <asp:BoundColumn DataField="Situacao" HeaderText="Situação"></asp:BoundColumn>
            <asp:BoundColumn DataField="Progresso" HeaderText="Progresso"></asp:BoundColumn>
            <asp:BoundColumn DataField="Duracao" HeaderText="Duracao"></asp:BoundColumn>
            <asp:BoundColumn DataField="TempoDecorrido" HeaderText="Tempo decorrido"></asp:BoundColumn>
            <asp:TemplateColumn HeaderText="Ação">
                <ItemTemplate>
                    <a runat="server" href='<%# "/Player/?file=" + Eval("NomeArquivo") %>' target="_blank">Tocar</a>
                </ItemTemplate>
            </asp:TemplateColumn>

        </Columns>

    </asp:DataGrid>
    <asp:ObjectDataSource ID="ObjectDataSource1" runat="server"></asp:ObjectDataSource>
</asp:Content>


