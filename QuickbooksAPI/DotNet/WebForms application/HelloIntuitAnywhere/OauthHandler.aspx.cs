﻿using System;
using System.Configuration;
using System.Globalization;
using System.Web;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using HelloIntuitAnywhere.Utilities;
using Intuit.Ipp.Core;

namespace HelloIntuitAnywhere
{
    /// <summary>
    /// This flow is invoked when the the user selects a QuickBooks company and then clicks Authorize to 
    /// grant this app access to that company's data. 
    /// Behind the scenes, this app exchanges tokens with the Intuit OAuth service and then 
    /// stores the authorized access token in a session store. (use persistent store such as a database in real time scenarios.)  
    /// A valid access token indicates that the user has connected your app to a specific company.  
    /// (Connections are important because Intuit charges you according to how many active connections 
    /// users have made with your app.)  Later, when your app calls Data Services for QuickBooks, 
    /// it fetches the access token from the persistent store and includes the token in the 
    /// HTTP request header.  
    /// </summary>
    public partial class OauthHandler : System.Web.UI.Page
    {
        /// <summary>
        /// OAuthVerifyer, RealmId, DataSource
        /// </summary>
        private String _oauthVerifyer, _realmid;

        /// <summary>
        /// Action Results for Index, OAuthToken, OAuthVerifyer and RealmID is recieved as part of Response
        /// and are stored inside Session object for future references
        /// NOTE: Session storage is only used for demonstration purpose only.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event Args.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString.HasKeys())
            {
                // This value is used to Get Access Token.
                _oauthVerifyer = Request.QueryString["oauth_verifier"].ToString(CultureInfo.InvariantCulture);

                _realmid = Request.QueryString["realmId"].ToString(CultureInfo.InvariantCulture);
                HttpContext.Current.Session["realm"] = _realmid;

                //If dataSource is QBO call QuickBooks Online Services, else call QuickBooks Desktop Services
                switch(Request.QueryString["dataSource"].ToString(CultureInfo.InvariantCulture))
                {
                    case "QBO":
                        HttpContext.Current.Session["intuitServiceType"] = IntuitServicesType.QBO;
                        break;
                    default:
                        HttpContext.Current.Session["intuitServiceType"] = IntuitServicesType.QBD;
                        break;
                }

                //Stored in a session for demo purposes.
                //Production applications should securely store the Access Token
                getAccessToken();

                // This value is used to redirect to Default.aspx from Cleanup page when user clicks on ConnectToInuit widget.
                Session["RedirectToDefault"] = true;
            }
            else
            {
                Response.Write("No OAuth token was received");
            }
        }

        /// <summary>
        /// Gets the Access Token
        /// </summary>
        private void getAccessToken()
        {
            IOAuthSession clientSession = CreateSession();
            try
            {
                IToken accessToken = clientSession.ExchangeRequestTokenForAccessToken((IToken)Session["requestToken"], _oauthVerifyer);
                Session["accessToken"] = accessToken.Token;

                // Add flag to session which tells that accessToken is in session
                Session["Flag"] = true;

                // Remove the Invalid Access token since we got the new access token
                HttpContext.Current.Session.Remove("InvalidAccessToken");
                Session["accessTokenSecret"] = accessToken.TokenSecret;
            }
            catch (Exception ex)
            {
                //Handle Exception if token is rejected or exchange of Request Token for Access Token failed. 
                throw ex;
            }
        }

        /// <summary>
        /// Creates the OAuth Session using Consumer key
        /// </summary>        
        /// <returns>OAuth Session.</returns>
        private IOAuthSession CreateSession()
        {
            OAuthConsumerContext consumerContext = new OAuthConsumerContext
            {
                ConsumerKey = ConfigurationManager.AppSettings["consumerKey"].ToString(CultureInfo.InvariantCulture),
                ConsumerSecret = ConfigurationManager.AppSettings["consumerSecret"].ToString(CultureInfo.InvariantCulture),
                SignatureMethod = SignatureMethod.HmacSha1
            };

            return new OAuthSession(consumerContext,
                                            Constants.OauthEndPoints.IdFedOAuthBaseUrl + Constants.OauthEndPoints.UrlRequestToken,
                                            Constants.OauthEndPoints.IdFedOAuthBaseUrl,
                                             Constants.OauthEndPoints.IdFedOAuthBaseUrl + Constants.OauthEndPoints.UrlAccessToken);
        }
    }
}