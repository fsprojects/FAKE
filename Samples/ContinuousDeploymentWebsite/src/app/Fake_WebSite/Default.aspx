<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
         CodeBehind="Default.aspx.cs" Inherits="Fake_WebSite._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <h2>
        Welcome to FAKE!
    </h2>
    <p>
        To learn more about FAKE visit <a href="https://github.com/forki/FAKE/" title="ASP.NET Website">github.com/forki/FAKE/</a>.
    </p>
    <p>Website version <%= Version %></p>
</asp:Content>