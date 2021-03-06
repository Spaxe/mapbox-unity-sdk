namespace Mapbox.Unity {
	using UnityEngine;
	using System.IO;
	using System;
	using System.Net;
	using Mapbox.Geocoding;
	using Mapbox.Directions;
	using Mapbox.Platform;
	using Mapbox.Unity.Utilities;

	/// <summary>
	/// Object for retrieving an API token and making http requests.
	/// Contains a lazy <see cref="T:Mapbox.Geocoding.Geocoder">Geocoder</see> and a lazy <see cref="T:Mapbox.Directions.Directions">Directions</see> for convenience.
	/// </summary>
	public class MapboxAccess : IFileSource {

		private readonly string _accessPath = Path.Combine(Application.streamingAssetsPath, Constants.Path.TOKEN_FILE);

		static MapboxAccess _instance = new MapboxAccess();


		/// <summary>
		/// The singleton instance.
		/// </summary>
		public static MapboxAccess Instance {
			get {
				return _instance;
			}
		}

		MapboxAccess() {
			ValidateMapboxAccessFile();
			LoadAccessToken();
		}


		/// <summary>
		/// The Mapbox API access token. 
		/// See <see href="https://www.mapbox.com/mapbox-unity-sdk/docs/01-mapbox-api-token.html">Mapbox API Congfiguration in Unity</see>.
		/// </summary>
		private string _accessToken;
		public string AccessToken {
			get {
				return _accessToken;
			}
			set {
				if (string.IsNullOrEmpty(value)) {
					throw new InvalidTokenException("Please configure your access token in the menu!");
				}
				_accessToken = value;
			}
		}


		private void ValidateMapboxAccessFile() {
#if !UNITY_ANDROID
			if (!Directory.Exists(Application.streamingAssetsPath) || !File.Exists(_accessPath)) {
				throw new InvalidTokenException("Please configure your access token in the menu!");
			}
#endif
		}


		/// <summary>
		/// Loads the access token from <see href="https://docs.unity3d.com/Manual/StreamingAssets.html">StreamingAssets</see>.
		/// </summary>
		private void LoadAccessToken() {
#if UNITY_EDITOR || !UNITY_ANDROID
			AccessToken = File.ReadAllText(_accessPath);
#else
            AccessToken = LoadMapboxAccess();
#endif
		}


		/// <summary>
		/// Android-specific token file loading.
		/// </summary>
		private string LoadMapboxAccess() {

			var request = new WWW(_accessPath);

			// Implement a custom timeout - just in case
			var timeout = Time.realtimeSinceStartup + 5f;
			while (!request.isDone) {
				if (Time.realtimeSinceStartup > timeout) {
					throw new InvalidTokenException("Could not load access token!");
				}
#if !NETFX_CORE
                System.Threading.Thread.Sleep(10);
#endif
			}
			return request.text;
		}


		/// <summary>
		/// Makes an asynchronous url query.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="url">URL.</param>
		/// <param name="callback">Callback.</param>
		public IAsyncRequest Request(string url, Action<Response> callback, int timeout = 10) {

			var uriBuilder = new UriBuilder(url);
			string accessTokenQuery = "access_token=" + AccessToken;

			if (uriBuilder.Query != null && uriBuilder.Query.Length > 1) {
				uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + accessTokenQuery;
			} else {
				uriBuilder.Query = accessTokenQuery;
			}
			return IAsyncRequestFactory.CreateRequest(uriBuilder.ToString(), callback, timeout);
		}


		class InvalidTokenException : Exception {
			public InvalidTokenException(string message) : base(message) {
			}
		}


		/// <summary>
		/// Lazy geocoder.
		/// </summary>
		Geocoder _geocoder;
		public Geocoder Geocoder {
			get {
				if (_geocoder == null) {
					_geocoder = new Geocoder(this);
				}
				return _geocoder;
			}
		}


		/// <summary>
		/// Lazy Directions.
		/// </summary>
		Directions _directions;
		public Directions Directions {
			get {
				if (_directions == null) {
					_directions = new Directions(this);
				}
				return _directions;
			}
		}
	}
}
