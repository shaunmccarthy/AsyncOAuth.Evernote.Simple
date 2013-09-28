AsyncOAuth.Evernote.Simple - 0.1
================================

AsyncOAuth.Evernote.Simple - Simple OAuth for Evernote (C# / .Net)

Summary
-------

This library is used to simplify the process of authorizing your application to
be granted access to a user's Evernote Repository. While there are plenty of 
OAuth libraries for .net out there, and there is an Evernote SDK for C#, 
there's no real quality examples out there on how to bring them together in a 
seemless way (especially in a web context).

This library is based off of AsyncOAuth - https://github.com/neuecc/AsyncOAuth

I outline how this project was created on my blog @ http://neofight.shaunmccarthy.com/

Projects
--------

There are two projects in this repository:

 * AsyncOAuth.Evernote.Simple - The library to simplify the OAuth process with 
   Evernote in C#
 * AsyncOAuth.Evernote.Simple.SampleMVC - A sample MVC application showing you
   how to authenticate against Evernote using MVC
   
How To:
-------

*Preparation*

 * You can request an API Key here: http://dev.evernote.com/doc/
 * You should also create an account on the evernote sandbox here: 
    https://sandbox.evernote.com/Registration.action
 * You will need to download / clone this repository
 * After opening the project file, either restore nuget packages, or 
   install AsyncOAuth with:
       Install-Package AsyncOAuth

*Calling The Library*

	// Configure the Authorizer with the URL of the Evernote service,
	// your key, and your secret.
	var EvernoteAuthorizer = new EvernoteAuthorizer(
	    "https://sandbox.evernote.com", 
		"slyrp-1234",
		"7acafe123456badb123");
	
    // First of all, get a request token from Evernote - this causes a 
	// webrequest from your server to Evernote.
	// The callBackUrl is the URL you want the user to return to once
	// they validate the app
    var requestToken = EvernoteAuthorizer.GetRequestToken(callBackUrl);
	
	// Persist this token, as we are going to redirect the user to 
	// Evernote to Authorize this app
	Session["RequestToken"] = requestToken;
	
	// Generate the Evernote URL that we will redirect the user to in
	// order to 
    var callForwardUrl = EvernoteAuthorizer.BuildAuthorizeUrl(requestToken);
	
	// Redirect the user (e.g. MVC)
	return Redirect(callForwardUrl);
	
	// ... Once the user authroizes the app, they get redirected to callBackUrl
	
	// where we parse the request parameter oauth_validator and finally get
	// our credentials
	// null = they didn't authorize us
	var credentials = EvernoteAuthorizer.ParseAccessToken(
	    Request.QueryString["oauth_verifier"], 
		Session["RequestToken"] as RequestToken);
		
	// Example of how to use the credential with Evernote SDK
    var noteStoreUrl = EvernoteCredentials.NotebookUrl;
    var noteStoreTransport = new THttpClient(new Uri(noteStoreUrl));
    var noteStoreProtocol = new TBinaryProtocol(noteStoreTransport);
    var noteStore = new NoteStore.Client(noteStoreProtocol);
    List<Notebook> notebooks = client.listNotebooks(EvernoteCredentials.AuthToken);

*Optional*

For simplicity's sake, OAuthUtility.ComputeHash is initialized whenever you 
instantiate EvernoteAuthorizer. You can initialize it yourself in global.asax
and pass in a "false" at the end of the constructor to tell EvernoteAuthorizer
not to initialize the function. Ideally, you could check to see if ComputeHash
is set, but AsyncOAuth doesn't expose it's getter.

Running The Sample:
-------------------

Open up the AsyncOAuth.Evernote.Simple.SampleMVC and run it - the index page 
will tell you what you need to change to get the project running. The 
interesting parts can be found in HomeController.cs

You can see what things would look like without the library in RawController.cs

To Do:
------

Make the library asynnc again :) Feel free to contribute fixes.

Links:
------
 * Evernote API: http://dev.evernote.com/doc/
 * Evernote C# SDK: https://github.com/evernote/evernote-sdk-csharp
 * Evernote OAuth: http://dev.evernote.com/doc/articles/authentication.php
 * AsyncOAuth for .net / C#: https://github.com/neuecc/AsyncOAuth

Author:
-------

Shaun McCarthy, github@shaunmccarthy.com, http://www.shaunmccarthy.com
