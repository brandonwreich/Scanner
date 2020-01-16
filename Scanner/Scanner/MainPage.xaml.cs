using Newtonsoft.Json.Linq;
using Scanner.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;

namespace Scanner
{
    public partial class MainPage : ContentPage
    {
        //Delcare pages
        ZXingScannerPage scanPage;

        //Declare lists
        public IList<string> Isbns { get; set; }
        public IList<Book> Books { get; set; }

        public MainPage()
        {
            //Init
            InitializeComponent();
            Isbns = new List<string>();
            Books = new List<Book>();

            //Add click actions
            scanButton.Clicked += ScanButton_Clicked;
            compareButton.Clicked += CompareButton_Clicked;
            clearListButton.Clicked += ClearListButton_Clicked;
            viewListButton.Clicked += ViewListButton_Clicked;
        }

        //When clicked then you can scan a barcode
        private async void ScanButton_Clicked(object sender, EventArgs e)
        {
            //Init
            scanPage = new ZXingScannerPage();

            //When you get the result
            scanPage.OnScanResult += (result) => {
                
                //Stop scanning
                scanPage.IsScanning = false;

                //Display alert confirming scan
                Device.BeginInvokeOnMainThread(() => {
                    Navigation.PopModalAsync();
                    DisplayAlert("Scanning Complete", "Barcode: " + result.Text, "OK");

                    //Add isbn to list
                    Isbns.Add(result.Text);
                });
            };

            //Scan
            await Navigation.PushModalAsync(scanPage);
        }

        //When clicked compares the buyback options for each isbn
        private async void CompareButton_Clicked(object sender, EventArgs e)
        {
            //If there are no isbns scanned
            if (Isbns.Count == 0)
            {
                //Display alert asking to scan books
                await DisplayAlert("Data Error", "Please scan some barcodes before you search for buyback options!", "Sorry");
            }
            else
            {

                //Loop through isbns
                foreach (string isbn in Isbns)
                {
                    //Init
                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("authority", "www.bookfinder.com");
                    var response3 = await httpClient.GetAsync($"https://www.bookfinder.com/buyback/affiliate/{isbn}.mhtml");
                    var raw3 = await response3.Content.ReadAsStringAsync();
                    decimal highestOffer = 0;
                    string bookTitle = "";

                    //If connection is made
                    if (response3.IsSuccessStatusCode)
                    {
                        //Init
                        var jobject = JObject.Parse(raw3);
                        bookTitle = jobject.GetValue("title").ToString();
                        var offers = jobject.GetValue("offers");
                        var test1 = offers.Children();

                        //Loop through buyback offers
                        foreach (var child in offers.Children().ToList())
                        {
                            //Init
                            var test = child.Children().FirstOrDefault();

                            //If offer is greater than highestOffer
                            if (test.Value<decimal>("buyback") == 1 && test.Value<decimal>("offer") > highestOffer)
                            {
                                //Set highesOffer
                                highestOffer = test.Value<decimal>("offer");
                            }
                        }
                    }

                    if(!checkInList(bookTitle, isbn))
                    {
                        //Add data to books list
                        Books.Add(new Book
                        {
                            Title = bookTitle,
                            Isbn = isbn,
                            Offer = highestOffer
                        });
                    }

                    //Create the result
                    string result = "Title: " + bookTitle + ". Offer: $" + highestOffer + ".";

                    //Display alert showing results
                    await DisplayAlert("Buyback Option", result, "Ok");
                }
            }
        }

        //When clicked clears all the data from the lists
        private async void ClearListButton_Clicked(object sender, EventArgs e)
        {
            //Clear lists
            Isbns.Clear();
            Books.Clear();

            //Display alert comfirming lists were cleared
            await DisplayAlert("Confirmation", "Lists have been cleared.", "Thanks");
        }

        //When clicked it shows the book offers
        private async void ViewListButton_Clicked(object sender, EventArgs e)
        {
            if (Books.Count == 0)
            {
                //Display alert asking to scan books
                DisplayAlert("Data Error", "You have no books! Please scan and compare some books.", "Sorry");
            }
            else
            {
                //Loop through all the books
                foreach (var book in Books)
                {
                    string result = "Title: " + book.Title + ". Offer: $" + book.Offer + ".";

                    //Display alert showing books
                    await DisplayAlert("Books", result, "Okay");
                }
            }
        }

        public bool checkInList(string bookTitle, string isbn)
        {
            foreach (Book book in Books)
            {
                if (bookTitle.Equals(book.Title) && isbn.Equals(book.Isbn))
                {
                    return true;
                }
            }

            return false;
        }
    }
}