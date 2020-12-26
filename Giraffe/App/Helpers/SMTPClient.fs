module App.Helpers.SMTPClient

open System.Net
open System.Net.Mail
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Giraffe
open Microsoft.Extensions.Configuration
open App.Helpers.HelperFunctions

type SMTPSettings = {
    ToAddress: string
    FromAddress: string
    Subject: string
    Port: int
    Body: string
    Host: string
    UserName: string
    Password: string
    EnableSSL: bool
}

let sendEmailAsync = fun settings ->
    task {
        use client = new SmtpClient(settings.Host, settings.Port)
        if isNull "pass" then
            client.UseDefaultCredentials <- true
        else
            client.Credentials <- NetworkCredential(settings.UserName, settings.Password)
            
        client.DeliveryMethod <- SmtpDeliveryMethod.Network;
        client.EnableSsl <- settings.EnableSSL
        
        let message =  new MailMessage(settings.FromAddress, settings.ToAddress)
        message.Body <- settings.Body
        message.Subject <- settings.Subject
        message.IsBodyHtml <- true
        
        return! client.SendMailAsync(message)
    }

let sendInviteEmailAsync = fun (ctx: HttpContext) fromAddress toAddress activationKey ->
    task{
        let config = ctx.GetService<IConfiguration>()
        //let url  = $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}/api/validation/invitation?activationKey={encodeBase64(activationKey)}";
        let url  = sprintf "%s://%s%s/api/validation/invitation?activationKey=%s" ctx.Request.Scheme (string ctx.Request.Host) (string ctx.Request.PathBase) (encodeBase64(activationKey))
        
        let body = sprintf @"<p></p><p></p><div class=""container - fluid""style=""background: #263a4f; color: #d9e0e8;""><div class=""row"" style=""margin-bottom: 30px;""><div class=""col-md-12 hero-login""><div class=""col-md-6 left50"" style=""margin: 30px;""><h1>Commercial Lending Center Suite</h1><p class=""lead"" style=""font-family: 'Open Sans', sans-serif; font-size:21px; font-weight:300"">Revolutionizing the way you find, grow, and manage loans! The Commercial Lending Center Suite empowers your financial institution to customize and improve processes, move beyond paper-based lending, and meet the needs of a wider variety of commercial borrowers&mdash;all while delivering a truly superior customer experience.</p><p class=""byline"">powered by:</p><a class=""col-md-2 logo"" style=""margin - bottom: 50px;"" href=""#""><img src=""https://clcstoragedev.blob.core.windows.net/b2c/commerciallending/azure-login/img/logo.png"" width=""220"" /></a><p>Hi,</p><p>Welcome to Commercial Lending Center Suite</p><p>To Complete your activation, click <a href=""%s"" style=""color: #ffffff"">here</a></p><p>CLCS Admin Team</p><p class=""help-text"">If you are having trouble logging in please contact JHA support <span class=""phone""><a style=""color: #fff7"" type=""tel"" href=""tel:18668795585"">Phone Number - 866.879.5585, Option 2</a></span> <span class=""email""><a type=""email"" href=""mailto:support@jhacorp.com"" style=""color: #ffffff"">Email - support@jhacorp.com</a></span></p></div></div></div></div>" url
        let smtpSettings = {
           ToAddress = toAddress
           FromAddress = fromAddress
           Subject = "Commercial Lending Center Suite Invitation Letter"
           Port = convert config.["SmtpSettings:Port"]
           Body = body
           Host = config.["SmtpSettings:Host"]
           UserName = config.["SmtpSettings:User"]
           Password = config.["SmtpSettings:Pass"]
           EnableSSL = bool config.["SmtpSettings:EnableSSL"] }
        
        return! sendEmailAsync smtpSettings
    }
    
