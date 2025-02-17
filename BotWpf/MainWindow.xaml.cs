﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using BotHandler;
using MahApps.Metro.Controls;
using Nethereum.Util;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
 
namespace BotWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private HttpClient _httpClient;

        private readonly string RugdocCheckUrl =
            "https://honeypot.api.rugdoc.io/api/honeypotStatus.js?address={0}&chain=bsc";

        public static string bnbcontrac = "0xbb4CdB9CBd36B01bD1cBaEBF2De08d9173bc095c";
        public static string busdcontrac = "0xe9e7cea3dedca5984780bafc599bd69add087d56";
      //  public static string bnbcontrac = "0xae13d989dac2f0debff460ac112a837c89baa7cd";
       // public static string busdcontrac = "0x7ef95a0fee0dd31b22626fa2e10ee6a223f8a684";
        public static string usdtContract = "0x55d398326f99059fF775485246999027B3197955";
   
        public  static string currentRouter = "pancake";
        private Bot _bot;
        public ObservableCollection<TxResult> resultsBuy = new ObservableCollection<TxResult>();
        public static decimal tokenBalanceD = 0;
        public static decimal bnbBalanceD = 0;
        public static decimal bnbPrice = 0;
        public static decimal tokenPriceLast = 0;
        public static decimal tokenPriceAmountLast = 0;
        public static decimal tokenPriceBuy = 0;
        public static decimal tokenPriceBuyc = 0;
        public static decimal tokenPriceSell = 0;
        public static MainWindow Instance;
        static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public static bool DoingSomething = false;
        public ConsoleWrapper Consola;
        
    public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            Consola = new ConsoleWrapper(Consola1);
            _bot = new Bot(BotHandler.Properties.Settings.Default.Wallet, BotHandler.Properties.Settings.Default.PK,
                BotHandler.Properties.Settings.Default.BSCNODE, "pancake",  Consola);
            buyTxGrid.ItemsSource = resultsBuy;
            _httpClient = new HttpClient();
            Task.Run(async () => await TokenBalance());
             walletAddress.Text= BotHandler.Properties.Settings.Default.Wallet ;
            bscNode.Text = BotHandler.Properties.Settings.Default.BSCNODE;
            pkAddress.Password = BotHandler.Properties.Settings.Default.PK;
            buyBtn.IsEnabled = false;
            sellBtn.IsEnabled = false;
            AproveBtn.IsEnabled = false;
            sellBtnAll.IsEnabled = false;
            sellBtnX.IsEnabled = false;
            
            ValDatos();
          
        }

    

    protected override void OnSourceInitialized(EventArgs e)
        {
                base.OnSourceInitialized(e);

                // Initialize the clipboard now that we have a window soruce to use
                var windowClipboardManager = new ClipboardManager(this);
                windowClipboardManager.ClipboardChanged += ClipboardChanged;
        }
        private void ClipboardChanged(object sender, EventArgs e)
        {
                // Handle your clipboard update here, debug logging example:
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        var text = Clipboard.GetText();
                        if (autopaste.IsOn)
                        {
                            if (text.IsValidEthereumAddressHexFormat())
                            { tokenBuy.Text = Clipboard.GetText();
                                autopaste.IsOn = false;
                            }

                           
                        }
                    }
                }
                catch (Exception exception)
                {
                    
                }
               
        }
        public async Task<bool> RugdocCheck(string token)
        {
                if (!auditT.IsOn)
                {
                    return true;
                }
                try
                {
                    var response = await _httpClient.GetAsync(string.Format(RugdocCheckUrl, token));
                    var rugdocStr = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(rugdocStr);
                    var valid = responseObject["status"].Value<string>().Equals("OK", StringComparison.InvariantCultureIgnoreCase);
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        Consola1.WriteOutput(Environment.NewLine + String.Format(
                            "Rugdoc check token {0} Status: {1} RugDoc Response: {2}", token, valid, rugdocStr), Colors.Red);
                    }), DispatcherPriority.Render);
               
                    return valid;
                }
                catch (Exception e)
                {
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        Consola1.WriteOutput(Environment.NewLine + e.Message+" Contact Support", Colors.Red);
                    }), DispatcherPriority.Render);
                    return false;
                }
        }
            public async void ValDatos()
            {

              
                    
                    
              
                    
                    if (BotHandler.Properties.Settings.Default.Wallet.IsValidEthereumAddressHexFormat() && BotHandler.Properties.Settings.Default.BSCNODE.Length > 4 &&
                        BotHandler.Properties.Settings.Default.PK.Length == 66)
                    {
                        AproveBtn.IsEnabled = true;
                    buyBtn.IsEnabled = true;
                        sellBtn.IsEnabled = true;
                        sellBtnAll.IsEnabled = true;
                        sellBtnX.IsEnabled = true;
                    }
                    else
                    {
                        Consola1.WriteOutput("Please set account info before continue.", Colors.Red);
                    }
            
               
                
                
          
            }

            public async Task TokenBalance()
            {
                decimal bnbBalance = 0;
                while (true)
                {
                    try
                    {
                        if (BotHandler.Properties.Settings.Default.Wallet.IsValidEthereumAddressHexFormat())
                        {
                            var bnbPrice2 = await _bot.TokenValueTask(bnbcontrac, busdcontrac);

                            bnbBalance = await _bot.GetAccountBalance();
                            await Application.Current.Dispatcher.BeginInvoke(
                                new Action(() => { bnbBalanceD = bnbBalance; }), DispatcherPriority.Render);
                            var buytoken = "";
                            bool route = false;
                            tokenBuy.Dispatcher.Invoke(
                                DispatcherPriority.Normal,
                                (ThreadStart) delegate { buytoken = fromBuy.Text; });
                            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                bnbName.Content = "BNB Balance";
                                pairBalance.Content = bnbBalanceD.ToString();
                                bnbPrice = (decimal) bnbPrice2;
                            }), DispatcherPriority.Render);

                            var text = "";
                            tokenBuy.Dispatcher.Invoke(
                                DispatcherPriority.Normal,
                                (ThreadStart) delegate { text = tokenBuy.Text; });
                            if (text.IsValidEthereumAddressHexFormat())
                            {
                                decimal value2 = 0;
                                var result = await _bot.TokenBalanceAsync(text);
                                BigDecimal value = 0;

                                if (buytoken == "BNB")
                                {
                                    value = await _bot.TokenValueTask(text, bnbcontrac);
                                    if (bnbPrice != 0)
                                        value = (decimal) (value * bnbPrice);
                                }
                                else
                                {
                                    if (buytoken == "BUSD")
                                    {
                                        value = await _bot.TokenValueTask(text, busdcontrac);

                                    }
                                    else
                                    {
                                        if (buytoken == "USDT")
                                        {
                                            value = await _bot.TokenValueTask(text, usdtContract);

                                        }
                                    }
                                }

                                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    tokenBalanceD = result;
                                    tokenBalance.Text = result.ToString();
                                    tokenVl.Content = value.ToString();
                                }), DispatcherPriority.Render);

                            }
                        }


                    }
                    catch (Exception e)
                    {
                        await Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            Consola1.WriteOutput(Environment.NewLine + e.Message + " Contact Support", Colors.Red);
                        }), DispatcherPriority.Render);
                    }

                    await Task.Delay(500);
                }
            }
 
            private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                var value =  (ComboBox)sender;
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    tokenPair1.Content =value.Text;
                }), DispatcherPriority.Render);
            }

            private void PreviewTextInput(object sender, TextCompositionEventArgs e)
            {
                Regex regex = new Regex("^[,][0-9]+$|^[0-9]*[,]{0,1}[0-9]*$");
                e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
            }
            private void PreviewTextInputInt(object sender, TextCompositionEventArgs e)
            {
                Regex regex = new Regex("[^0-9.-]+");
                e.Handled = regex.IsMatch(e.Text);
            }

            private async void buyBtn_Click(object sender, RoutedEventArgs e)
            {
                if (ValidarDatosBuy())
                {
                           if (!DoingSomething)
                           {    Buy();
                                DoingSomething = true;
                           }
                }
                else

                {
                    Consola1.WriteOutput("Please look your inputs",Colors.Red);
                }

            }

            public async void Buy()
            {
                decimal slip = 0;
                var pair = new TokenSearchResult();
                AproveBtn.IsEnabled = false;
            buyBtn.IsEnabled = false;
                sellBtn.IsEnabled = false;
                sellBtnAll.IsEnabled = false;
                sellBtnX.IsEnabled = false;
                List<string> lista = new List<string>();
                if (pairRoute.IsOn)
                {

                    pair = await _bot.GetNewPairs(tokenBuy.Text, true, pairRoute.IsOn,
                        cancellationTokenSource.Token);

                   // Consola1.WriteOutput(Environment.NewLine + "Probando venta", Colors.Green);
                    if (pair.isFound)
                    {
                       

                        PairReserve reserves = new PairReserve();
                       
                        
                            reserves = await _bot.GetReservesRouteTask(pair, cancellationTokenSource.Token);
                    Consola1.WriteOutput(Environment.NewLine + "Pair: " + reserves.Pair,
                        Colors.Aqua);
                    if (reserves.Pair == bnbcontrac)
                    {
                        tokenPair1.Content = "BNB";
                        tokenPair1.Foreground = new SolidColorBrush((Colors.Green));
                        fromBuy.Text = "BNB";
                    }
                    else
                    {
                        if (reserves.Pair == busdcontrac)
                        {
                            tokenPair1.Content = "BUSD";
                            tokenPair1.Foreground = new SolidColorBrush((Colors.Green));
                            fromBuy.Text = "BUSD";
                        }
                        else
                        {
                            if (reserves.Pair == usdtContract)
                            {
                                tokenPair1.Content = "USDT";
                                tokenPair1.Foreground = new SolidColorBrush((Colors.Green));
                                fromBuy.Text = "USDT";
                            }
                        }
                    }


                    
                        Consola1.WriteOutput(
                            Environment.NewLine + "Pair: " + reserves.Pair +
                            " Balance " + reserves.Reserver,
                            Colors.Aqua);
                        

                            if (pair.tokenPair == bnbcontrac)
                            {
                                lista.Add(bnbcontrac);
                                lista.Add(tokenBuy.Text);

                                if (!slipMax_Copy.IsOn)
                                {

                                    var amount = await _bot.SlippageTask(busdcontrac,
                                        tokenBuy.Text,
                                        amountBuy.Text);
                                    slip = ((decimal) (Web3.Convert.FromWei(amount,
                                               UnitConversion.EthUnit.Ether))) *
                                           ((100 - decimal.Parse(SlipBuy.Text)) / 100);
                                }
                            }
                            else
                            {
                                if (pair.tokenPair == busdcontrac)
                                {
                                    lista.Add(bnbcontrac);
                                    lista.Add(busdcontrac);
                                    lista.Add(tokenBuy.Text);
                                    if (!slipMax_Copy.IsOn)
                                    {
                                        var input = decimal.Parse(amountBuy.Text) *
                                                    bnbPrice;
                                        var amount = await _bot.SlippageTask(
                                            busdcontrac, tokenBuy.Text,
                                            input.ToString());
                                        slip = ((decimal) (Web3.Convert.FromWei(amount,
                                                   UnitConversion.EthUnit.Ether))) *
                                               ((100 - decimal.Parse(SlipBuy.Text)) /
                                                100);
                                    }
                                }
                                else
                                {
                                    if (pair.tokenPair == usdtContract)
                                    {
                                        lista.Add(bnbcontrac);
                                        lista.Add(usdtContract);
                                        lista.Add(tokenBuy.Text);

                                        if (!slipMax_Copy.IsOn)
                                        {
                                            var input = decimal.Parse(amountBuy.Text) *
                                                        bnbPrice;
                                            var amount = await _bot.SlippageTask(
                                                usdtContract, tokenBuy.Text,
                                                input.ToString());
                                            slip = ((decimal) (Web3.Convert.FromWei(amount,
                                                       UnitConversion.EthUnit.Ether))) *
                                                   ((100 - decimal.Parse(SlipBuy.Text)) /
                                                    100);
                                        }
                                    }
                                    else
                                    {
                                        Consola1.WriteOutput(Environment.NewLine + "Error searching pair for token, contact support", Colors.Red);
                                           
                                    }
                                }
                                    
                            }
                            if (lista.Count > 0)
                            {
                                if (await (RugdocCheck(tokenBuy.Text)))
                                {
                                    
                                        Consola1.WriteOutput(Environment.NewLine +
                                                             "Buying " + tokenName.Content + " amount: " +
                                                             amountBuy.Text,
                                            Colors.Green);
                                            var resultado = await _bot.DeBNBaToken(
                                                amountBuy.Text, slip,
                                                lista,
                                                gweiAmount.Text);
                                            if (resultado.result == "Success")
                                            {
                                                
                                                tokenPriceBuy = resultado.price* bnbPrice;
                                                tokenbuyPrice.Content = tokenPriceBuy.ToString();
                                if (approveAfter.IsOn)
                                {
                                    Consola1.WriteOutput(Environment.NewLine +
                                                         ($"Approving Token on {0}", currentRouter), Colors.Green);
                                    var result = await _bot.AproveTask(tokenBuy.Text, gweiAmount.Text);

                                    resultsBuy.Add(result);
                                    if (result.result == "Success")
                                    {
                                        Consola1.WriteOutput(Environment.NewLine + "Contract Approved", Colors.Green);

                                    }
                                    else
                                    {

                                        Consola1.WriteOutput(Environment.NewLine + "Failed to Approve, see if token exist and see txhash", Colors.Green);
                                    }
                                }
                                if (autoSellOn.IsOn)
                                {
                                    DoingSomething = false;
                                    amountSell.Text = resultado.value.ToString();
                                    AutosellTask(int.Parse(delay.Text), decimal.Parse(profitT.Text));
                                }

                            }
                                            resultsBuy.Add(resultado);
                                    
                                   
                                }

                            
                        }
                    
                    
                    }
                    else
                    {
                        Consola1.WriteOutput(
                            Environment.NewLine + "Error searching pair for token, please retry or contact support", Colors.Red);
                    }
                }
                else
                {
                    if (fromBuy.Text == "BNB")
                    {
                        bool pairF = false;
                        decimal reserves = 0;
                        if (!hasLiquidity.IsOn)
                        {
                            pair = await _bot.GetNewPairs(tokenBuy.Text, true, false,
                                cancellationTokenSource.Token);
                            if (pair.isFound)
                            {
                                pairF = true;
                                reserves = await _bot.GetReservesTask(pair.pairAddress, 1,
                                    cancellationTokenSource.Token);
                            }
                        }
                        else
                        {
                            reserves = 121;
                            pairF = true;
                        }

                        if (reserves > 100 && pairF)
                        {
                            lista.Add(bnbcontrac);
                            lista.Add(tokenBuy.Text);
                            tokenPair1.Foreground = new SolidColorBrush((Colors.Blue));
                            tokenPair1.Content = "BNB";
                            if (!slipMax_Copy.IsOn)
                            {
                                var amount = await _bot.SlippageTask(bnbcontrac,
                                    tokenBuy.Text,
                                    amountBuy.Text);

                                slip = ((decimal) (Web3.Convert.FromWei(amount,
                                           UnitConversion.EthUnit.Ether))) *
                                       ((100 - decimal.Parse(SlipBuy.Text)) / 100);
                            }
                        }
                        else
                        {
                            Consola1.WriteOutput(Environment.NewLine + "Error searching pair for token, contact support", Colors.Red);
                        }
                    }
                    else
                    {
                        bool pairF = false;
                        if (fromBuy.Text == "BUSD")
                        {

                            decimal reserves = 0;
                            if (!hasLiquidity.IsOn)
                            {
                                pair = await _bot.GetNewPairs(tokenBuy.Text, false, false,
                                    cancellationTokenSource.Token);

                                if (pair.isFound)
                                {
                                    pairF = true;
                                    reserves = await _bot.GetReservesTask(
                                        pair.pairAddress, 1,
                                        cancellationTokenSource.Token);
                                }
                            }
                            else
                            {
                                reserves = 121;
                                pairF = true;
                            }

                            if (reserves > 100 && pairF)
                            {
                                lista.Add(bnbcontrac);
                                lista.Add(busdcontrac);
                                lista.Add(tokenBuy.Text);
                                tokenPair1.Foreground = new SolidColorBrush((Colors.Blue));
                                tokenPair1.Content = "BUSD";
                                if (!slipMax_Copy.IsOn)
                                {
                                    var input = decimal.Parse(amountBuy.Text) * bnbPrice;
                                    var amount = await _bot.SlippageTask(busdcontrac,
                                        tokenBuy.Text,
                                        input.ToString());
                                    slip = ((decimal) (Web3.Convert.FromWei(amount,
                                               UnitConversion.EthUnit.Ether))) *
                                           ((100 - decimal.Parse(SlipBuy.Text)) / 100);
                                }
                            }
                            else
                            {
                                Consola1.WriteOutput(Environment.NewLine + "Error searching pair for token, contact support", Colors.Red);
                            }
                        }
                        else
                        {
                            if (fromBuy.Text == "USDT")
                            {
                                decimal reserves = 0;
                                if (!hasLiquidity.IsOn)
                                {
                                    pair = await _bot.GetPairTask(tokenBuy.Text,
                                        usdtContract, cancellationTokenSource.Token);

                                    if (pair.isFound)
                                    {
                                        pairF = true;
                                        reserves = await _bot.GetReservesTask(
                                            pair.pairAddress, 1,
                                            cancellationTokenSource.Token);
                                    }
                                }
                                else
                                {
                                    reserves = 121;
                                    pairF = true;
                                }

                                if (reserves > 100 && pairF)
                                {
                                    lista.Add(bnbcontrac);
                                    lista.Add(usdtContract);
                                    lista.Add(tokenBuy.Text);
                                    tokenPair1.Foreground = new SolidColorBrush((Colors.Blue));
                                    tokenPair1.Content = "BUSD";
                                    if (!slipMax_Copy.IsOn)
                                    {
                                        var input = decimal.Parse(amountBuy.Text) * bnbPrice;
                                        var amount = await _bot.SlippageTask(usdtContract,
                                            tokenBuy.Text,
                                            input.ToString());
                                        slip = ((decimal) (Web3.Convert.FromWei(amount,
                                                   UnitConversion.EthUnit.Ether))) *
                                               ((100 - decimal.Parse(SlipBuy.Text)) / 100);
                                    }
                                }
                                else
                                {
                                    Consola1.WriteOutput(Environment.NewLine + "Error searching pair for token, contact support", Colors.Red);
                                }
                            }
                        }
                    }
                    if (lista.Count > 0)
                    {
                        if (await (RugdocCheck(tokenBuy.Text)))
                        {
                            
                                Consola1.WriteOutput(Environment.NewLine + "Buying " + tokenName.Content + " amount: " + amountBuy.Text, Colors.Green);
                               var resultado = await _bot.DeBNBaToken(amountBuy.Text, slip, lista, gweiAmount.Text);
                                if (resultado.result == "Success")
                                {
                                   
                            tokenPriceBuy = resultado.price * bnbPrice;
                                    tokenbuyPrice.Content= tokenPriceBuy.ToString();
                                    resultsBuy.Add(resultado);
                                    if (approveAfter.IsOn)
                                    { Consola1.WriteOutput(Environment.NewLine+
                                                           ($"Approving Token on {0}", currentRouter),Colors.Green);
                                        var result = await _bot.AproveTask(tokenBuy.Text, gweiAmount.Text);

                                         resultsBuy.Add(result);
                                          if (result.result == "Success")
                                          {
                                           Consola1.WriteOutput(Environment.NewLine + "Contract Approved", Colors.Green);

                                          }
                                          else
                                          {

                                              Consola1.WriteOutput(Environment.NewLine + "Failed to Approve, see if token exist and see txhash", Colors.Green);
                                          }
                                    }
                                    if (autoSellOn.IsOn)
                                    {
                                        DoingSomething = false;
                                        amountSell.Text = resultado.value.ToString();
                                        AutosellTask(int.Parse(delay.Text), decimal.Parse(profitT.Text));
                                    }

                                }
                                
                            
                        }
                    }
                }

                DoingSomething = false;
                AproveBtn.IsEnabled = true;
            buyBtn.IsEnabled = true;
                sellBtn.IsEnabled = true;
                sellBtnAll.IsEnabled = true;
                sellBtnX.IsEnabled = true;
            }
          
        public bool ValidarDatosBuy()
            {
                if (amountBuy.Text != "" && gweiAmount.Text != "")
                {
                    if (tokenBuy.Text.IsValidEthereumAddressHexFormat())
                    {
                        if (decimal.Parse(amountBuy.Text) > 0 &&
                            decimal.Parse((string)pairBalance.Content) != decimal.Parse(amountBuy.Text))
                        {
                            if (int.Parse(gweiAmount.Text) >= 5)
                            {
                                if (int.Parse(sellxText.Text) > 0 && int.Parse(sellxText.Text) <= 100)
                                {
                                    if (int.Parse(SlipBuy.Text) > 5 && int.Parse(SlipBuy.Text)<100)
                                    {
                                        if (int.Parse(gweiAmount.Text) > 0 && decimal.Parse(amountBuy.Text) > 0)
                                        {

                                            if (decimal.Parse(profitT.Text) == 0 || decimal.Parse(profitT.Text) > 1)
                                            {
                                               
                                                    return true;
                                           

                                            }
                                            else
                                            {Consola1.WriteOutput(Environment.NewLine+"Enter 0 on profit for disable or >1 for enable no use for 1>X>0",Colors.Red);
                                                    return false;
                                            }
                                          
                                        }
                                        else
                                        {
                                            Consola1.WriteOutput(Environment.NewLine + "Set valid amount or gas", Colors.Red);
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        Consola1.WriteOutput(Environment.NewLine + "Slip % incorrect, min 5 max 100", Colors.Red);
                                        return false;
                                    }
                                }
                                else
                                {
                                    Consola1.WriteOutput(Environment.NewLine + "Sell % incorrect", Colors.Red);
                                    return false;
                                }
                            }
                            else
                            {

                                Consola1.WriteOutput(Environment.NewLine + "Error: Gas amount too low or incorrect.",
                                    Colors.Red);
                                return false;

                            }
                        }
                        else
                        {
                            Consola1.WriteOutput(Environment.NewLine + "Error: Amount not enoght or incorrect", Colors.Red);
                            return false;
                        }
                    }
                    else
                    {
                        Consola1.WriteOutput(Environment.NewLine + "Error: Input a valid Token Address", Colors.Red);
                        return false;
                    }
                }
                else
                {
                    Consola1.WriteOutput(Environment.NewLine + "Error: Input a valid Gas and amount", Colors.Red);
                    return false;
                }
            }

            private async void AproveBtn_Click(object sender, RoutedEventArgs e)
            {
                AproveBtn.IsEnabled = false;
                buyBtn.IsEnabled = false;
                sellBtn.IsEnabled = false;
                sellBtnAll.IsEnabled = false;
                sellBtnX.IsEnabled = false;
                if(!DoingSomething)
                    if (tokenBuy.Text.Length == 42)
                    {
                        DoingSomething = true;
                        var result = await _bot.AproveTask(tokenBuy.Text, gweiAmount.Text);
                    
                        resultsBuy.Add(result);
                        if (result.result== "Success")
                        {
                            Consola1.WriteOutput(Environment.NewLine+"Contract Approved",Colors.Green);
                          
                        }
                        else
                        {
                        
                            Consola1.WriteOutput(Environment.NewLine + "Failed to Approve, see if token exist and see txhash", Colors.Green);
                        }
           
                    }
                    else
                    {
                        Consola1.WriteOutput(Environment.NewLine+"Wrong Address",Colors.Red);
                    }

                DoingSomething = false;
                AproveBtn.IsEnabled = true;
                buyBtn.IsEnabled = true;
                sellBtn.IsEnabled = true;
                sellBtnAll.IsEnabled = true;
                sellBtnX.IsEnabled = true;

            }

            private  void sellBtn_Click(object sender, RoutedEventArgs e)
            {
               
                    Sell();
                
                
            }

            public async void Sell()
            {
                decimal slip = 0;
                AproveBtn.IsEnabled = false;
                buyBtn.IsEnabled = false;
                sellBtn.IsEnabled = false;
                sellBtnAll.IsEnabled = false;
                sellBtnX.IsEnabled = false;
                DoingSomething = true;
                List<string> path = new List<string>();
                TxResult result = new TxResult();
                if (amountSell.Text != "" && gweiAmount_Copy.Text != ""&& SlipSell.Text!=""&&sellxText.Text!="")
                {
                    if (tokenBuy.Text.Length == 42)
                    {
                        if (decimal.Parse(amountSell.Text) > 0)
                        {
                            if (int.Parse(gweiAmount_Copy.Text) > 4)
                            {
                                if (int.Parse(sellxText.Text) > 0&& int.Parse(sellxText.Text) < 100)
                                {
                                    if (int.Parse(SlipSell.Text) > 3&&int.Parse(SlipSell.Text) <100)
                                    {
                                        Consola1.WriteOutput(
                                            Environment.NewLine + "Selling " + tokenName.Content + " amount: " + amountSell.Text,
                                            Colors.Green);
                                        if (fromBuy.Text == "BNB")
                                        {
                                            if (!slipMax.IsOn)
                                            {
                                   
                                                var amount = await _bot.SlippageTask(tokenBuy.Text,bnbcontrac,
                                                    amountSell.Text);
                                                slip = ((decimal)(Web3.Convert.FromWei(amount,
                                                    UnitConversion.EthUnit.Ether))) * ((100 - decimal.Parse(SlipSell.Text)) / 100);
                                            }

                                            {
                                                path.Add(tokenBuy.Text);
                                                path.Add(bnbcontrac);
                                                
                                            }

                                        }
                                        else
                                        {
                                            if (fromBuy.Text == "BUSD")
                                            {
                                                if (!slipMax.IsOn)
                                                {
                                        
                                                    var amount = await _bot.SlippageTask( tokenBuy.Text,busdcontrac,
                                                        amountSell.Text);
                                                    slip = ((decimal)(Web3.Convert.FromWei(amount,
                                                        UnitConversion.EthUnit.Ether))) * ((100 - decimal.Parse(SlipSell.Text)) / 100)*bnbPrice;
                                                }
                                            
                                                path.Add(tokenBuy.Text);
                                                path.Add(busdcontrac);
                                                path.Add(bnbcontrac);
                                            
                                    
                                            }
                                            else
                                            {
                                                if (fromBuy.Text == "USDT")
                                                {
                                                    if (!slipMax.IsOn)
                                                    {
                                                        var amount = await _bot.SlippageTask(tokenBuy.Text, usdtContract,
                                                            amountSell.Text);
                                                        slip = ((decimal)(Web3.Convert.FromWei(amount,
                                                            UnitConversion.EthUnit.Ether))) * ((100 - decimal.Parse(SlipSell.Text)) / 100)* bnbPrice;
                                                    }
                                                    {
                                                        path.Add(tokenBuy.Text);
                                                        path.Add(usdtContract);
                                                        path.Add(bnbcontrac);
                                                       
                                                    }
                                                }
                                            }

                                        }

                                    }
                                    else
                                    {
                                        Consola1.WriteOutput(Environment.NewLine + "Slip % incorrect, min 5 max 100", Colors.Red);
                             
                                    }
                                }
                                else
                                {
                                    Consola1.WriteOutput(Environment.NewLine + "Sell % incorrect", Colors.Red);
                           
                                }
                            }
                            else
                            {
                                Consola1.WriteOutput(Environment.NewLine + "Gas too low or incorrect", Colors.Red);
                            
                            }
                        }
                        else
                        {
                            Consola1.WriteOutput(Environment.NewLine + "Gas too low or higher than Balance", Colors.Red);
                   
                        }
                    }
                    else
                    {
                        Consola1.WriteOutput(Environment.NewLine + "Wrong address", Colors.Red);
                     
                    }
                }
                else
                {
                    Consola1.WriteOutput(Environment.NewLine + "Wrong input on gas and amount", Colors.Red);
                    
                }

                if (path.Count > 0)
                {
                    
                    

                    result = await _bot.DeTokenABNB(amountSell.Text, slip, path, gweiAmount_Copy.Text);
                    resultsBuy.Add(result);
                    if (result.result == "Success")
                    {



                        tokenPriceSell = result.price * bnbPrice;
                        tokensellPrice.Content = tokenPriceSell.ToString();
                        if (tokenPriceSell > tokenPriceBuy)
                        {
                            tokensellPrice.Foreground = new SolidColorBrush(Colors.Green);
                        }
                        else
                        {
                            tokensellPrice.Foreground = new SolidColorBrush(Colors.Red);
                        }


                    }
                
            }
            DoingSomething = false;
                AproveBtn.IsEnabled = true;
                buyBtn.IsEnabled = true;
                sellBtn.IsEnabled = true;
                sellBtnAll.IsEnabled = true;
                sellBtnX.IsEnabled = true;
              
            }
            private async void tokenBuy_TextChanged(object sender, TextChangedEventArgs e)
            {
                var text = (TextBox) sender;
                try
                {
                    if (text.Text.IsValidEthereumAddressHexFormat())
                    {
                        var name = await _bot.GetNameTask(text.Text);
                        await Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            tokenName.Content = name.ToString() + " Value"; tokenNameB.Content = name.ToString() + " Balance";
                        }), DispatcherPriority.Render);
                    }
                }
                catch (Exception exception)
                {
                    
                }
           
            }

            public async Task AutosellTask(int delay, decimal profit)
            {
                var calc = tokenPriceBuy * profit;
                try
                {
                    await Task.Delay(delay*1000);
                    if (profit > 0)
                    {
                        while (true)
                        {
                            if (tokenPriceLast >= calc)
                            {
                                if (!DoingSomething)
                                {
                                    Sell();
                                }
                                break;
                            }

                            await Task.Delay(300);
                        }

                    }
                    else
                    {
                        if (!DoingSomething)
                            Sell();

                    }
            }
                catch (Exception e)
                {
                    Consola1.WriteOutput("Failed AutoSell, use manual sell",Colors.Red);
                    
                }
                
            }
            private void DataGridCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            {
                try
                {
                    if (sender.GetType() == typeof(DataGridCell))
                    {
                        var cell = (DataGridCell)sender;
                        var item = (TxResult)cell.DataContext;

                        // The cell content depends on the column used.
                        if (cell.Column.Header.ToString() == "TXHASH")
                        {
                            if (item.txHash != null)
                            {
                                ;
                                OpenUrl("https://bscscan.com/tx/" + item.txHash);

                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                
                }
           


            }
        
            private void OpenUrl(string url)
            {
                try
                {
                    Process.Start(url);
                }
                catch
                {
                    // hack because of this: https://github.com/dotnet/corefx/issues/10361
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        url = url.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", url);
                    }
                    else
                    {
                        throw;
                    }
                }
            }


            private async void stop_Click(object sender, RoutedEventArgs e)
            {
                cancellationTokenSource.Cancel();
                await Task.Delay(100);
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
                
            }

            private void Button_Click_2(object sender, RoutedEventArgs e)
            {
                BotHandler.Properties.Settings.Default.Wallet = walletAddress.Text;
                BotHandler.Properties.Settings.Default.BSCNODE = bscNode.Text;
                BotHandler.Properties.Settings.Default.PK = pkAddress.Password;
                BotHandler.Properties.Settings.Default.Save();
                _bot.ChangeNode(bscNode.Text);
                _bot.ChangePK(pkAddress.Password);
                _bot.ChangeWallet(walletAddress.Text);
                ValDatos();
            }

            private void sellBtnAll_Click(object sender, RoutedEventArgs e)
            {
                amountSell.Text = tokenBalanceD.ToString();
                Sell();
            }
            private void buyTxGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
            {
                try
                {
                    if (sender.GetType() == typeof(DataGrid))
                    {
                        var data = (DataGrid)sender;
                        var cell = data.CurrentCell;
                        var item = (TxResult)cell.Item;

                        // The cell content depends on the column used.
                        if (cell.Column.Header.ToString() == "TXHASH")
                        {
                            if (item.txHash != null)
                            {
                                ;
                                OpenUrl("https://bscscan.com/tx/" + item.txHash);

                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                
                }
            
            }

            private void Button_Click_3(object sender, RoutedEventArgs e)
            {
                Clipboard.SetText(donateAddress.Text);
            }

            private void sellBtnX_Click(object sender, RoutedEventArgs e)
            {
                decimal sellamount = tokenBalanceD - (tokenBalanceD * ((100 - decimal.Parse(sellxText.Text)) / 100));
                amountSell.Text = sellamount.ToString();
                Sell();
            }

            private void tokenVl_ContentChanged(object sender, EventArgs e)
            {var lab = (CustomLabel) sender;
                decimal count;
                try
                {
                    if (decimal.Parse(lab.Content.ToString(), CultureInfo.InvariantCulture) > tokenPriceLast)
                    {
                        tokenPriceLast = decimal.Parse(lab.Content.ToString(), CultureInfo.InvariantCulture);
                        tokenVl.Foreground = new SolidColorBrush(Colors.LightSeaGreen);

                    }
                    if (decimal.Parse(lab.Content.ToString(), CultureInfo.InvariantCulture) < tokenPriceLast)
                    {
                        tokenPriceLast = decimal.Parse(lab.Content.ToString(),CultureInfo.InvariantCulture);
                        tokenVl.Foreground = new SolidColorBrush(Colors.Red);

                    }
                    if (tokenBalance.Text != "0")
                    {
                        count = tokenPriceLast * decimal.Parse(tokenBalance.Text);
                        Balance_Value.Text = count.ToString();
                    }
                    else
                    {
                        Balance_Value.Text = "0";
                    }
                }
                catch (Exception exception)
                {
                
                }

            }

     
            private void Balance_Value_TextChanged(object sender, TextChangedEventArgs e)
            {
                var lab = (TextBox)sender;
                try
                {
                    if (decimal.Parse(lab.Text) > tokenPriceLast)
                    {
                        tokenPriceAmountLast = decimal.Parse(lab.Text);
                        Balance_Value.Foreground = new SolidColorBrush(Colors.LightSeaGreen);

                    }
                    if (decimal.Parse(lab.Text) < tokenPriceLast)
                    {
                        tokenPriceAmountLast = decimal.Parse(lab.Text);
                        Balance_Value.Foreground = new SolidColorBrush(Colors.Red);

                    }
                }
                catch (Exception exception)
                {

                }

            }

            private void tokenBalance_TextChanged(object sender, TextChangedEventArgs e)
            {

            }

        private async void SwapperSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var text = (ComboBox) sender;
            await Task.Delay(100);
            switch (text.Text)
            {
                case "PancakeSwap":
                   
                    _bot.ChangeSwap("pancake");
                    break;
                case "ApeSwap":

                    _bot.ChangeSwap("ape");
                    break;
                case "BiSwap":

                    _bot.ChangeSwap("bi");
                    break;
                case "test":
                    _bot.ChangeSwap("test");
                    break;
                default:

                    _bot.ChangeSwap("pancake");

                    break;
            }
        }

        private void Consola1_Loaded(object sender, RoutedEventArgs e)
        {

        }

      
    }




    public class AddressValid : ValidationRule
    {
    

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (((string)value).Length == 42)
            {
               return new ValidationResult(true, null); 
            }
            else
            {

                return new ValidationResult(false,
                    "Please enter a valid address");
            }
        }
    }
    public class AmountValid : ValidationRule
    {


        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                var text = double.Parse((string)value);
                if (text > 0)
                {
                    return new ValidationResult(true, null);
                }
                else
                {
                    return new ValidationResult(false,
                        "Please enter a valid Amount, use , not . for decimals");
                }
            }
            catch (Exception e)
            {return new ValidationResult(false,
                "Please enter a valid Amount, use , not . for decimals");
                
            }
            
        }
    }
   
    public class GasValid : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                var text = int.Parse((string)value);
                if (text>= 5 && text < 500)
                {
                    return new ValidationResult(true, null);
                }
                else
                {
                    if (text > 200)
                    {
                        return new ValidationResult(false,
                            "Too high gas, may be too expensive");
                    }
                    return new ValidationResult(false,
                        "Please enter a valid gas amount min 5 recommended 20");
                }
            }
            catch (Exception e)
            {
                
                
                return new ValidationResult(false,
                    "Please enter a valid gas amount min 5 recommended 20");
            }
            
        }
    }
    public class SellValid : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                var text = int.Parse((string)value);
                if (text >= 1 && text <= 100)
                {
                    return new ValidationResult(true, null);
                }
                else
                {
                    
                    return new ValidationResult(false,
                        "Please enter a valid % sell amount min 1 recommended 100, integer only");
                }
            }
            catch (Exception e)
            {


                return new ValidationResult(false,
                    "Please enter a valid % sell amount min 1 recommended 100, integer only");
            }

        }
    }
    public class ProfitValid : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                var text = decimal.Parse((string)value);
                if (text >= 1 && text <= 100)
                {
                    return new ValidationResult(true, null);
                }
                else
                {

                    return new ValidationResult(false,
                        "Please enter a valid profit amount min 1.001 Max 100x");
                }
            }
            catch (Exception e)
            {


                return new ValidationResult(false,
                    "Please enter a valid profit amount min 1.001 Max 100x");
            }

        }
    }
    public class SlipValid : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                var text = int.Parse((string)value);
                if (text >= 5 && text < 500)
                {
                    return new ValidationResult(true, null);
                }
                else
                {
                    if (text > 200)
                    {
                        return new ValidationResult(false,
                            "Too high slip, may as well use max slip");
                    }
                    return new ValidationResult(false,
                        "Please enter a valid % slip amount min 5 recommended 40");
                }
            }
            catch (Exception e)
            {


                return new ValidationResult(false,
                    "Please enter a valid % slip amount min 5 recommended 40");
            }

        }
    }
    public class CustomLabel : Label
    {
        public event EventHandler ContentChanged;

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (ContentChanged != null)
                ContentChanged(this, EventArgs.Empty);
        }
    }
    public class MyDataSource
    {
        public MyDataSource()
        {
            address = "0x";
            address2 = "0x";
            wallet = "0x";
            amount1 = "0";
            amount2 = "0";
            gas1 = "25";
            gas2 = "25";
            sell = "0";
            Slip = "50";
            Slip2 = "50";
            Profit = "0";
            delay = "0";
            sell2 = "10";
        }

        public string address { get; set; }
        public string address2 { get; set; }
        public string wallet { get; set; }
        public string amount1 { get; set; }
        public string amount2 { get; set; }
        public string gas1 { get; set; }
        public string gas2 { get; set; }
        public string sell { get; set; }
        public string Slip { get; set; }
        public string sell2 { get; set; }
        public string Profit { get; set; }
        public string Slip2 { get; set; }
        public string delay { get; set; }
    }



    internal static class NativeMethods
    {
        // See http://msdn.microsoft.com/en-us/library/ms649021%28v=vs.85%29.aspx
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        // See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);
    }
    public class ClipboardManager
    {
        public event EventHandler ClipboardChanged;

        public ClipboardManager(Window windowSource)
        {
            HwndSource source = PresentationSource.FromVisual(windowSource) as HwndSource;
            if (source == null)
            {
                throw new ArgumentException(
                    "Window source MUST be initialized first, such as in the Window's OnSourceInitialized handler."
                    , nameof(windowSource));
            }

            source.AddHook(WndProc);

            // get window handle for interop
            IntPtr windowHandle = new WindowInteropHelper(windowSource).Handle;

            // register for clipboard events
            NativeMethods.AddClipboardFormatListener(windowHandle);
        }

        private void OnClipboardChanged()
        {
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }

        private static readonly IntPtr WndProcSuccess = IntPtr.Zero;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                OnClipboardChanged();
                handled = true;
            }

            return WndProcSuccess;
        }
    }
}
