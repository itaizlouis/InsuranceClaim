﻿using AutoMapper;
using Insurance.Domain;
using Insurance.Service;
using InsuranceClaim.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static InsuranceClaim.Controllers.CustomerRegistrationController;
using System.Configuration;
using System.Web.Configuration;
using System.IO;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace InsuranceClaim.Controllers
{
    public class AgentMotorController : Controller
    {
        private ApplicationUserManager _userManager;

        string AdminEmail = WebConfigurationManager.AppSettings["AdminEmail"];
        string ZimnatEmail = WebConfigurationManager.AppSettings["ZimnatEmail"];
        public static string _AgentModule = "AgentModule";

        Insurance.Service.smsService objsmsService = new Insurance.Service.smsService();

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: AgentMotor
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CustomerDetail(int id=0)
        {

            if (id != -1) // -1 use for getting session value when click on back button
            {
                RemoveSession();
            }

            bool userLoggedin = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
            string path = Server.MapPath("~/Content/Countries.txt");
            var countries = System.IO.File.ReadAllText(path);
            var resultt = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject>(countries);
            ViewBag.Countries = resultt.countries.OrderBy(x => x.code.Replace("+", ""));

            //string paths = Server.MapPath("~/Content/Cities.txt");
            //var cities = System.IO.File.ReadAllText(paths);
            //var resultts = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObjects>(cities);
            //ViewBag.Cities = resultts.cities;

            ViewBag.Cities = InsuranceContext.Cities.All();



            if (id > 0) // if staff try to edit Qutation
            {
                SetCustomerValueIntoSession(id); // here id represent to summardetialid during edit the Qutation
            }


            if (userLoggedin && id == 0)
            {
                var customerModel = new CustomerModel();
                var _User = UserManager.FindById(User.Identity.GetUserId().ToString());


                var _customerData = InsuranceContext.Customers.All(where: $"UserId ='{User.Identity.GetUserId().ToString()}'").FirstOrDefault();
                var customerData = (CustomerModel)Session["CustomerDataModal"];


                var role = UserManager.GetRoles(_User.Id.ToString()).FirstOrDefault();

                ViewBag.CurrentUserRole = role;

                if ((role != null && (role != "AgentStaff" && role != "Renewals")))
                {
                    if (customerData != null)
                    {
                        var User = UserManager.FindById(customerData.UserID);
                        customerModel.AddressLine1 = customerData.AddressLine1;
                        customerModel.AddressLine2 = customerData.AddressLine2;
                        customerModel.City = customerData.City;
                        customerModel.Id = customerData.Id;
                        customerModel.Country = customerData.Country;
                        customerModel.Zipcode = customerData.Zipcode;
                        customerModel.Gender = customerData.Gender;
                        customerModel.PhoneNumber = customerData.PhoneNumber;
                        customerModel.NationalIdentificationNumber = customerData.NationalIdentificationNumber;
                        customerModel.DateOfBirth = customerData.DateOfBirth;
                        customerModel.EmailAddress = customerData.EmailAddress;
                        customerModel.FirstName = customerData.FirstName;
                        customerModel.LastName = customerData.LastName;
                        customerModel.CountryCode = customerData.CountryCode;
                        customerModel.IsCustomEmail = customerData.IsCustomEmail;
                    }
                    else
                    {
                        customerModel.AddressLine1 = _customerData.AddressLine1;
                        customerModel.AddressLine2 = _customerData.AddressLine2;
                        customerModel.City = _customerData.City;
                        customerModel.Id = _customerData.Id;
                        customerModel.Country = _customerData.Country;
                        customerModel.Zipcode = _customerData.Zipcode;
                        customerModel.Gender = _customerData.Gender;
                        customerModel.PhoneNumber = _User.PhoneNumber;
                        customerModel.NationalIdentificationNumber = _customerData.NationalIdentificationNumber;
                        customerModel.DateOfBirth = _customerData.DateOfBirth;
                        customerModel.EmailAddress = _User.Email;
                        customerModel.FirstName = _customerData.FirstName;
                        customerModel.LastName = _customerData.LastName;
                        customerModel.CountryCode = _customerData.Countrycode;
                        customerModel.CustomerId = _customerData.CustomerId;
                        customerModel.IsActive = _customerData.IsActive;
                        customerModel.UserID = _customerData.UserID;
                        customerModel.IsCustomEmail = _customerData.IsCustomEmail;
                    }
                    customerModel.Zipcode = "00263";

                    Session["CustomerDataModal"] = customerModel; // for admin
                }
                customerModel.Zipcode = "00263";
                RemoveSession();



                return View(customerModel);
            }
            else
            {
                var customerData = (CustomerModel)Session["CustomerDataModal"];
                var customerModel = new CustomerModel();
                customerModel.Zipcode = "00263";
                if (customerData != null)
                {
                    var User = UserManager.FindById(customerData.UserID);
                    customerModel.AddressLine1 = customerData.AddressLine1;
                    customerModel.AddressLine2 = customerData.AddressLine2;
                    customerModel.City = customerData.City;
                    customerModel.Id = customerData.Id;
                    customerModel.Country = customerData.Country;
                    customerModel.Zipcode = customerData.Zipcode;
                    customerModel.Gender = customerData.Gender;
                    customerModel.PhoneNumber = customerData.PhoneNumber;
                    customerModel.NationalIdentificationNumber = customerData.NationalIdentificationNumber;
                    customerModel.DateOfBirth = customerData.DateOfBirth;
                    customerModel.EmailAddress = customerData.EmailAddress;
                    customerModel.FirstName = customerData.FirstName;
                    customerModel.LastName = customerData.LastName;
                    customerModel.CountryCode = customerData.CountryCode;
                    customerModel.IsCustomEmail = customerData.IsCustomEmail;
                }
                return View(customerModel);
            }
        }


        public void SetCustomerValueIntoSession(int summaryId)
        {
            Session["ICEcashToken"] = null;
            Session["issummaryformvisited"] = true;
            Session["SummaryDetailId"] = summaryId;

            var summaryDetail = InsuranceContext.SummaryDetails.Single(summaryId);
            var Cusotmer = InsuranceContext.Customers.Single(summaryDetail.CustomerId);
            CustomerModel custModel = AutoMapper.Mapper.Map<Customer, CustomerModel>(Cusotmer);

            if (Cusotmer != null)
            {
                var dbUser = UserManager.Users.FirstOrDefault(c => c.Id == Cusotmer.UserID);
                if (dbUser != null)
                {
                    custModel.EmailAddress = dbUser.Email; ;
                }
            }
            Session["CustomerDataModal"] = custModel;
        }


        private void RemoveSession()
        {
            Session.Remove("CustomerDataModal");
            Session.Remove("PolicyData");
            Session.Remove("VehicleDetails");
            Session.Remove("SummaryDetailed");
            Session.Remove("CardDetail");
            Session.Remove("issummaryformvisited");
            Session.Remove("PaymentId");
            Session.Remove("InsuranceId");


        }


        [HttpPost]
        public async Task<JsonResult> SaveCustomerData(CustomerModel model, string buttonUpdate)
        {
            if (ModelState.IsValid)
            {
                bool userLoggedin = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;

                if (userLoggedin)
                {

                    //var AllUsers = UserManager.Users.ToList();
                    //var isExist = AllUsers.Any(p => p.Email.ToLower() == model.EmailAddress.ToLower() || p.UserName.ToLower() == model.EmailAddress);
                    //if (isExist)
                    //{
                    //    return Json(new { IsError = false, error = "Email " + model.EmailAddress + " already exists." }, JsonRequestBehavior.AllowGet);
                    //}

                    if (User.IsInRole("Staff") || User.IsInRole("Renewals"))
                    {
                        //if (buttonUpdate != null)
                        //{
                        //    AddOrUpdateCustomerInformation(model);

                        //    return Json(new { IsError = false, error = "Sucessfully update" }, JsonRequestBehavior.AllowGet);
                        //}

                        var email = LoggedUserEmail();

                        if (email == model.EmailAddress)
                        {
                            return Json(new { IsError = false, error = "Staff and customer email can not be same" }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    Session["CustomerDataModal"] = model;
                    return Json(new { IsError = true, error = "" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var AllUsers = UserManager.Users.ToList();//.FirstOrDefault(p=>p.Email== model.EmailAddress);
                    var isExist = AllUsers.Any(p => p.Email.ToLower() == model.EmailAddress.ToLower() || p.UserName.ToLower() == model.EmailAddress);
                    if (isExist)
                    {
                        return Json(new { IsError = false, error = "Email " + model.EmailAddress + " already exists." }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        Session["CustomerDataModal"] = model;
                        return Json(new { IsError = true, error = "" }, JsonRequestBehavior.AllowGet);
                    }
                }

            }
            return Json(new { IsError = false, error = TempData["ErrorMessage"].ToString() }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult RiskDetail(int? id = 1)
        {
            // summaryDetailId: it's represent to Qutation edit

            if (Session["SummaryDetailId"] != null)
            {
                SetValueIntoSession(Convert.ToInt32(Session["SummaryDetailId"]));
                Session["SummaryDetailId"] = null;
            }


            if (Session["CustomerDataModal"] == null)
            {
                // return RedirectToAction("Index", "CustomerRegistration");
                return Redirect("/CustomerRegistration/Index");
            }


            ViewBag.Products = InsuranceContext.Products.All(where: "Active = 'True' or Active is Null").ToList();
            //var ePaymentTermData = from ePaymentTerm e in Enum.GetValues(typeof(ePaymentTerm))
            //                       select new
            //                       {
            //                           ID = (int)e,
            //                           Name = e.ToString()
            //                       };

            //ViewBag.ePaymentTermData = new SelectList(ePaymentTermData, "ID", "Name");
            // ViewBag.PaymentTermId = InsuranceContext.PaymentTerms.All().ToList();
            ViewBag.PaymentTermId = InsuranceContext.PaymentTerms.All(where: "IsActive = 'True' or IsActive is Null").ToList();
            ViewBag.TaxClass = InsuranceContext.VehicleTaxClasses.All().ToList().Take(7);

            var eExcessTypeData = from eExcessType e in Enum.GetValues(typeof(eExcessType))
                                  select new
                                  {
                                      ID = (int)e,
                                      Name = e.ToString()
                                  };

            ViewBag.eExcessTypeData = new SelectList(eExcessTypeData, "ID", "Name");

            int RadioLicenseCosts = 0;
            //  int RadioLicenseCosts = Convert.ToInt32(InsuranceContext.Settings.All().Where(x => x.keyname == "RadioLicenseCost").Select(x => x.value).FirstOrDefault());
            var PolicyData = (PolicyDetail)Session["PolicyData"];
            //Id is policyid from Policy detail table
            var viewModel = new RiskDetailModel();
            var service = new VehicleService();

            ViewBag.VehicleUsage = service.GetAllVehicleUsage();

            viewModel.VehicleUsage = 0;
            viewModel.NumberofPersons = 0;
            viewModel.AddThirdPartyAmount = 0.00m;
            viewModel.RadioLicenseCost = Convert.ToDecimal(RadioLicenseCosts);
            var makers = service.GetMakers();



            //    var coverTypes = service.GetCoverType();
            ViewBag.CoverType = service.GetCoverType(); //coverTypes.Where(c=>c.Id==(int) eCoverType.Comprehensive).ToList();
            ViewBag.AgentCommission = service.GetAgentCommission();


            var data1 = (from p in InsuranceContext.BusinessSources.All().ToList()
                         join f in InsuranceContext.SourceDetails.All().ToList()
                         on p.Id equals f.BusinessId
                         select new
                         {
                             Value = f.Id,
                             Text = f.FirstName + " " + f.LastName + " - " + p.Source
                         }).ToList();

            List<SelectListItem> listdata = new List<SelectListItem>();
            foreach (var item in data1)
            {
                SelectListItem sli = new SelectListItem();
                sli.Value = Convert.ToString(item.Value);
                sli.Text = item.Text;
                listdata.Add(sli);
            }
            ViewBag.Sources = new SelectList(listdata, "Value", "Text");
            //ViewBag.Sources = InsuranceContext.BusinessSources.All();

            ViewBag.Currencies = InsuranceContext.Currencies.All(where: $"IsActive = 'True'");

            // viewModel.CurrencyId = 7; // default "RTGS$" selected // for test server

            viewModel.CurrencyId = 6; // default "RTGS$" selected // for live server

           // viewModel.CoverTypeId = (int)eCoverType.Comprehensive;

            ViewBag.Makers = makers;
            viewModel.isUpdate = false;
            //TempData["Policy"] = service.GetPolicy(id);
            if (makers.Count > 0)
            {
                var model = service.GetModel(makers.FirstOrDefault().MakeCode);
                ViewBag.Model = model;

            }

            viewModel.NoOfCarsCovered = 1;
            if (Session["VehicleDetails"] != null)
            {
                var list = (List<RiskDetailModel>)Session["VehicleDetails"];
                viewModel.NoOfCarsCovered = list.Count + 1;

            }

            if (id > 0)
            {
                var list = (List<RiskDetailModel>)Session["VehicleDetails"];
                if (list != null && list.Count > 0 && (list.Count >= id))
                {
                    var data = (RiskDetailModel)list[Convert.ToInt32(id - 1)];
                    if (data != null)
                    {
                        viewModel.AgentCommissionId = data.AgentCommissionId;
                        viewModel.ChasisNumber = data.ChasisNumber;
                        viewModel.CoverEndDate = data.CoverEndDate;
                        viewModel.CoverNoteNo = data.CoverNoteNo;
                        viewModel.CoverStartDate = data.CoverStartDate;
                        viewModel.CoverTypeId = data.CoverTypeId;
                        viewModel.CubicCapacity = (int)Math.Round(data.CubicCapacity.Value, 0);
                        viewModel.CustomerId = data.CustomerId;
                        viewModel.EngineNumber = data.EngineNumber;
                        //viewModel.Equals = data.Equals;
                        viewModel.Excess = (int)Math.Round(data.Excess, 0);
                        viewModel.ExcessType = data.ExcessType;
                        viewModel.MakeId = data.MakeId;
                        viewModel.ModelId = data.ModelId;
                        viewModel.NoOfCarsCovered = id;
                        viewModel.OptionalCovers = data.OptionalCovers;
                        viewModel.PolicyId = data.PolicyId;
                        viewModel.Premium = data.Premium;
                        viewModel.PremiumWithDiscount = data.Premium + data.Discount;

                        viewModel.RadioLicenseCost = (int)Math.Round(data.RadioLicenseCost == null ? 0 : data.RadioLicenseCost.Value, 0);
                        viewModel.Rate = data.Rate;
                        viewModel.RegistrationNo = data.RegistrationNo;
                        viewModel.StampDuty = data.StampDuty;
                        viewModel.SumInsured = (int)Math.Round(data.SumInsured == null ? 0 : data.SumInsured.Value, 0);
                        viewModel.VehicleColor = data.VehicleColor;
                        viewModel.VehicleUsage = data.VehicleUsage;
                        viewModel.VehicleYear = data.VehicleYear;
                        viewModel.Id = data.Id;
                        viewModel.ZTSCLevy = data.ZTSCLevy;
                        viewModel.NumberofPersons = data.NumberofPersons;
                        viewModel.PassengerAccidentCover = data.PassengerAccidentCover;
                        viewModel.IsLicenseDiskNeeded = data.IsLicenseDiskNeeded;
                        viewModel.ExcessBuyBack = data.ExcessBuyBack;
                        viewModel.RoadsideAssistance = data.RoadsideAssistance;
                        viewModel.MedicalExpenses = data.MedicalExpenses;
                        viewModel.Addthirdparty = data.Addthirdparty;
                        viewModel.AddThirdPartyAmount = data.AddThirdPartyAmount;
                        viewModel.ExcessAmount = data.ExcessAmount;
                        viewModel.ExcessAmount = data.ExcessAmount;
                        viewModel.ExcessBuyBackAmount = data.ExcessBuyBackAmount;
                        viewModel.MedicalExpensesAmount = data.MedicalExpensesAmount;
                        viewModel.MedicalExpensesPercentage = data.MedicalExpensesPercentage;
                        viewModel.PassengerAccidentCoverAmount = data.PassengerAccidentCoverAmount;
                        viewModel.PassengerAccidentCoverAmountPerPerson = data.PassengerAccidentCoverAmountPerPerson;
                        viewModel.PaymentTermId = data.PaymentTermId;
                        viewModel.ProductId = data.ProductId;
                        viewModel.IncludeRadioLicenseCost = data.IncludeRadioLicenseCost;
                        viewModel.RenewalDate = data.RenewalDate;
                        viewModel.TransactionDate = data.TransactionDate;
                        viewModel.AnnualRiskPremium = data.AnnualRiskPremium;
                        viewModel.TermlyRiskPremium = data.TermlyRiskPremium;
                        viewModel.QuaterlyRiskPremium = data.QuaterlyRiskPremium;
                        viewModel.Discount = data.Discount;
                        viewModel.VehicleLicenceFee = Convert.ToDecimal(data.VehicleLicenceFee);
                        viewModel.InsuranceId = data.InsuranceId;
                        viewModel.BusinessSourceDetailId = data.BusinessSourceDetailId;
                        viewModel.CurrencyId = data.CurrencyId;

                        viewModel.isUpdate = true; //commented on "31 oct"
                                                   //viewModel.isUpdate = false; // 02_feb_2019

                        viewModel.vehicleindex = Convert.ToInt32(id);
                        viewModel.TaxClassId = data.TaxClassId;

                        var ser = new VehicleService();
                        var model = ser.GetModel(data.MakeId);
                        ViewBag.Model = model;
                    }
                }
            }


            return View(viewModel);
        }

        public void SetValueIntoSession(int summaryId)
        {
            Session["ICEcashToken"] = null;
            Session["issummaryformvisited"] = true;

            Session["SummaryDetailId"] = summaryId;

            var summaryDetail = InsuranceContext.SummaryDetails.Single(summaryId);
            var SummaryVehicleDetails = InsuranceContext.SummaryVehicleDetails.All(where: $"SummaryDetailId={summaryId}").ToList();
            var vehicle = InsuranceContext.VehicleDetails.Single(SummaryVehicleDetails[0].VehicleDetailsId);
            var policy = InsuranceContext.PolicyDetails.Single(vehicle.PolicyId);
            var product = InsuranceContext.Products.Single(Convert.ToInt32(policy.PolicyName));


            Session["PolicyData"] = policy;

            List<RiskDetailModel> listRiskDetail = new List<RiskDetailModel>();
            foreach (var item in SummaryVehicleDetails)
            {
                var _vehicle = InsuranceContext.VehicleDetails.Single(item.VehicleDetailsId);
                RiskDetailModel riskDetail = Mapper.Map<VehicleDetail, RiskDetailModel>(_vehicle);
                listRiskDetail.Add(riskDetail);
            }
            Session["VehicleDetails"] = listRiskDetail;

            SummaryDetailModel summarymodel = Mapper.Map<SummaryDetail, SummaryDetailModel>(summaryDetail);
            summarymodel.Id = summaryDetail.Id;
            Session["SummaryDetailed"] = summarymodel;

        }

        [HttpPost]
        public ActionResult GenerateQuote(RiskDetailModel model, string btnAddVehicle = "")
        {

            if (model.NumberofPersons == null)
            {
                model.NumberofPersons = 0;
            }

            if (model.AddThirdPartyAmount == null)
            {
                model.AddThirdPartyAmount = 0.00m;
            }

            // if policy id is not null it mean's it will be update

            //if (model.chkAddVehicles == false && model.PolicyId != 0)
            //    model.isUpdate = true;
            //else if (model.chkAddVehicles)
            //    model.isUpdate = false;

            int selectedIndex = 0;

            // Submit & Add More Vehicle


            ModelState.Remove("SumInsured");




            if (model.isUpdate)
            {
                try
                {
                    model.Id = 0;

                    //if (!model.IncludeRadioLicenseCost)
                    //{
                    //    model.RadioLicenseCost = 0.00m;
                    //}

                    if (ModelState.IsValid)
                    {
                        List<RiskDetailModel> listriskdetailmodel = new List<RiskDetailModel>();
                        if (Session["VehicleDetails"] != null)
                        {
                            List<RiskDetailModel> listriskdetails = (List<RiskDetailModel>)Session["VehicleDetails"];
                            if (listriskdetails != null && listriskdetails.Count > 0)
                            {
                                listriskdetailmodel = listriskdetails;
                            }
                        }
                        model.Id = listriskdetailmodel[model.vehicleindex - 1].Id;
                        model.CustomerId = listriskdetailmodel[model.vehicleindex - 1].CustomerId;
                        model.InsuranceId = listriskdetailmodel[model.vehicleindex - 1].InsuranceId;
                        model.RegistrationNo = listriskdetailmodel[model.vehicleindex - 1].RegistrationNo;

                        if (!model.IncludeRadioLicenseCost)
                            model.RadioLicenseCost = 0;

                        listriskdetailmodel[model.vehicleindex - 1] = model;



                       // Session["VehicleDetails"] = listriskdetailmodel;
                    }
                    else
                    {

                    }

                    if (btnAddVehicle == "")
                    {
                        return RedirectToAction("SummaryDetail");
                    }
                    else
                    {
                        // while click on updat button or submit buttton without add more.
                        if (User.IsInRole("Staff"))
                        {
                            return RedirectToAction("RiskDetail", "AgentMotor", new { id = 0 });
                        }
                        else
                        {
                            return RedirectToAction("RiskDetail", new { id = 0 });
                        }
                    }
                }
                catch (Exception ex)
                {
                  //  WriteLog(ex.Message);

                    if (User.IsInRole("Staff"))
                    {
                        return RedirectToAction("RiskDetail", "AgentMotor");
                    }
                    else
                    {
                        return RedirectToAction("RiskDetail");

                    }

                }

            }
            else
            {
                try
                {
                    if (model.chkAddVehicles)
                    {
                        DateTimeFormatInfo usDtfi = new CultureInfo("en-US", false).DateTimeFormat;
                        var service = new RiskDetailService();
                        var startDate = Request.Form["CoverStartDate"];
                        var endDate = Request.Form["CoverEndDate"];
                        if (!string.IsNullOrEmpty(startDate))
                        {
                            ModelState.Remove("CoverStartDate");
                            model.CoverStartDate = Convert.ToDateTime(startDate, usDtfi);
                        }
                        if (!string.IsNullOrEmpty(endDate))
                        {
                            ModelState.Remove("CoverEndDate");
                            model.CoverEndDate = Convert.ToDateTime(endDate, usDtfi);
                        }

                        if (ModelState.IsValid)
                        {
                            model.Id = 0;

                            List<RiskDetailModel> listriskdetailmodel = new List<RiskDetailModel>();
                            if (Session["VehicleDetails"] != null) // 06 march
                            {
                                List<RiskDetailModel> listriskdetails = (List<RiskDetailModel>)Session["VehicleDetails"];
                                if (listriskdetails != null && listriskdetails.Count > 0)
                                {
                                    listriskdetailmodel = listriskdetails;
                                }
                            }

                            if (!model.IncludeRadioLicenseCost) // 13_feb_2019
                                model.RadioLicenseCost = 0;


                            listriskdetailmodel.Add(model);
                         //   Session["VehicleDetails"] = listriskdetailmodel;

                            selectedIndex = listriskdetailmodel.Count();

                        }

                        if (User.IsInRole("AgentStaff"))
                        {
                            return RedirectToAction("RiskDetail", "AgentMotor", new { id = 0 });
                        }
                        else
                        {
                            return RedirectToAction("RiskDetail", new { id = 0 });

                        }

                    }
                    else
                    {

                        DateTimeFormatInfo usDtfi = new CultureInfo("en-US", false).DateTimeFormat;
                        var service = new RiskDetailService();
                        var startDate = Request.Form["CoverStartDate"];
                        var endDate = Request.Form["CoverEndDate"];
                        if (!string.IsNullOrEmpty(startDate))
                        {
                            ModelState.Remove("CoverStartDate");
                            model.CoverStartDate = Convert.ToDateTime(startDate, usDtfi);
                        }
                        if (!string.IsNullOrEmpty(endDate))
                        {
                            ModelState.Remove("CoverEndDate");
                            model.CoverEndDate = Convert.ToDateTime(endDate, usDtfi);
                        }
                        if (ModelState.IsValid)
                        {
                            model.Id = 0;

                            //if (!model.IncludeRadioLicenseCost)
                            //{
                            //    model.RadioLicenseCost = 0.00m;
                            //}


                            List<RiskDetailModel> listriskdetailmodel = new List<RiskDetailModel>();
                            if (Session["VehicleDetails"] != null)
                            {
                                List<RiskDetailModel> listriskdetails = (List<RiskDetailModel>)Session["VehicleDetails"];
                                if (listriskdetails != null && listriskdetails.Count > 0)
                                {
                                    listriskdetailmodel = listriskdetails;
                                }
                            }
                            model.Id = 0;

                            if (!model.IncludeRadioLicenseCost) // 13_feb_2019
                                model.RadioLicenseCost = 0;

                            listriskdetailmodel.Add(model);
                            Session["VehicleDetails"] = listriskdetailmodel;

                        }

                        return RedirectToAction("SummaryDetail");
                    }

                }
                catch (Exception ex)
                {
                //    WriteLog(ex.Message);
                    return RedirectToAction("SummaryDetail");
                }
            }
        }



        public ActionResult SummaryDetail(int summaryDetailId = 0, string paymentError = "")
        {


            if (Session["CustomerDataModal"] == null && summaryDetailId == 0)
            {
                // return RedirectToAction("Index", "CustomerRegistration");
                return Redirect("/CustomerRegistration/Index");
            }

            if (Session["VehicleDetails"] == null && summaryDetailId == 0)
            {
                //return RedirectToAction("RiskDetail", "CustomerRegistration");
                return Redirect("/CustomerRegistration/RiskDetail");
            }
            var model = new SummaryDetailModel();
            try
            {

                Session["issummaryformvisited"] = true;
                var summarydetail = (SummaryDetailModel)Session["SummaryDetailed"];
                SummaryDetailService SummaryDetailServiceObj = new SummaryDetailService();

                ViewBag.SummaryDetailId = summaryDetailId;


                var role = "";
                if (System.Web.HttpContext.Current.User.Identity.GetUserId() != null)
                {
                    role = UserManager.GetRoles(System.Web.HttpContext.Current.User.Identity.GetUserId()).FirstOrDefault();
                }


                ViewBag.CurrentUserRole = role;

                //if (summarydetail != null) // on 05-oct while editing qutation
                //{
                //    return View(summarydetail);
                //}



                var summary = new SummaryDetailService();
                var vehicle = (List<RiskDetailModel>)Session["VehicleDetails"];// summary.GetVehicleInformation(id);

                List<RiskDetailModel> vehicleList = new List<RiskDetailModel>();
                if (summaryDetailId != 0)
                {
                    model.CustomSumarryDetilId = summaryDetailId;
                    //vehicle = summary.GetVehicleInformation(id);
                    var summaryVichalList = InsuranceContext.SummaryVehicleDetails.All(where: $" SummaryDetailId='{summaryDetailId}'");

                    foreach (var item in summaryVichalList)
                    {
                        var vehicleDetails = InsuranceContext.VehicleDetails.Single(where: $" Id='{item.VehicleDetailsId}' and IsActive<>0");

                        if (vehicleDetails != null)
                        {

                            RiskDetailModel vehicleModel = Mapper.Map<VehicleDetail, RiskDetailModel>(vehicleDetails);

                            // vehicleModel.CurrencyName = 

                            var currency = InsuranceContext.Currencies.Single(where: $" Id='{vehicleModel.CurrencyId}' ");

                            if (currency != null)
                                vehicleModel.CurrencyName = currency.Name;



                            //vehicleModel.Premium = vehicleDetails.Premium;
                            //vehicleModel.ZTSCLevy = vehicleDetails.ZTSCLevy;
                            //vehicleModel.StampDuty = vehicleDetails.StampDuty;
                            //vehicleModel.IncludeRadioLicenseCost = vehicleDetails.IncludeRadioLicenseCost.Value;
                            //vehicleModel.RadioLicenseCost = vehicleDetails.RadioLicenseCost;
                            //vehicleModel.Discount = vehicleDetails.Discount;
                            //vehicleModel.SumInsured = vehicleDetails.SumInsured;
                            //vehicleModel.ExcessBuyBackAmount = vehicleDetails.ExcessBuyBackAmount;

                            //vehicleModel.MedicalExpensesAmount = vehicleDetails.MedicalExpensesAmount;
                            //vehicleModel.PassengerAccidentCoverAmount = vehicleDetails.PassengerAccidentCoverAmount;
                            //vehicleModel.RoadsideAssistanceAmount = vehicleDetails.RoadsideAssistanceAmount;

                            //vehicleModel.ExcessAmount = vehicleDetails.ExcessAmount;
                            //vehicleModel.PassengerAccidentCoverAmount = vehicleDetails.PassengerAccidentCoverAmount;
                            //vehicleModel.RoadsideAssistanceAmount = vehicleDetails.RoadsideAssistanceAmount;
                            //vehicleModel.ModelId = vehicleDetails.ModelId;

                            if (!vehicleModel.IncludeRadioLicenseCost)
                                vehicleModel.RadioLicenseCost = 0;

                            vehicleList.Add(vehicleModel);
                        }
                    }
                    vehicle = vehicleList;
                    Session["VehicleDetails"] = vehicle;
                }

                var DiscountSettings = InsuranceContext.Settings.Single(where: $"keyname='Discount On Renewal'");
                model.CarInsuredCount = vehicle.Count;
                model.DebitNote = "INV" + Convert.ToString(SummaryDetailServiceObj.getNewDebitNote());

                //default selection 
                if (User.IsInRole("AgentStaff") || User.IsInRole("Renewals"))
                {
                    model.PaymentMethodId = 1;
                }
                else
                {
                    model.PaymentMethodId = 2;
                }


                model.PaymentTermId = 1;
                model.ReceiptNumber = "";
                model.SMSConfirmation = false;
                //model.TotalPremium = vehicle.Sum(item => item.Premium + item.ZTSCLevy + item.StampDuty + item.RadioLicenseCost);
                model.TotalPremium = 0.00m;
                model.TotalRadioLicenseCost = 0.00m;
                model.Discount = 0.00m;
                foreach (var item in vehicle)
                {
                    model.TotalPremium += item.Premium + item.ZTSCLevy + item.StampDuty + item.VehicleLicenceFee;
                    if (item.IncludeRadioLicenseCost)
                    {
                        model.TotalPremium += item.RadioLicenseCost;
                        model.TotalRadioLicenseCost += item.RadioLicenseCost;
                    }
                    model.Discount += item.Discount;


                    var currency = InsuranceContext.Currencies.Single(where: $" Id='{item.CurrencyId}' ");

                    if (currency != null)
                        item.CurrencyName = currency.Name;


                    //if (DiscountSettings.ValueType == Convert.ToInt32(eSettingValueType.percentage))
                    //{
                    //    var amountToCalculateDiscount = 0.00m;
                    //    switch (item.PaymentTermId)
                    //    {
                    //        case 1:
                    //            amountToCalculateDiscount = Convert.ToDecimal(item.AnnualRiskPremium);
                    //            break;
                    //        case 3:
                    //            amountToCalculateDiscount = Convert.ToDecimal(item.QuaterlyRiskPremium);
                    //            break;
                    //        case 4:
                    //            amountToCalculateDiscount = Convert.ToDecimal(item.TermlyRiskPremium);
                    //            break;
                    //    }
                    //    model.Discount += ((Convert.ToDecimal(DiscountSettings.value) * amountToCalculateDiscount) / 100);
                    //}
                    //if (DiscountSettings.ValueType == Convert.ToInt32(eSettingValueType.amount))
                    //{
                    //    model.Discount += Convert.ToDecimal(DiscountSettings.value);
                    //}
                }
                model.TotalRadioLicenseCost = Math.Round(Convert.ToDecimal(model.TotalRadioLicenseCost), 2);
                model.Discount = Math.Round(Convert.ToDecimal(model.Discount), 2);
                model.TotalPremium = Math.Round(Convert.ToDecimal(model.TotalPremium), 2);
                model.TotalStampDuty = Math.Round(Convert.ToDecimal(vehicle.Sum(item => item.StampDuty)), 2);
                model.TotalSumInsured = Math.Round(Convert.ToDecimal(vehicle.Sum(item => item.SumInsured)), 2);
                model.TotalZTSCLevies = Math.Round(Convert.ToDecimal(vehicle.Sum(item => item.ZTSCLevy)), 2);
                model.ExcessBuyBackAmount = Math.Round(Convert.ToDecimal(vehicle.Sum(item => item.ExcessBuyBackAmount)), 2);
                model.MedicalExpensesAmount = Math.Round(Convert.ToDecimal(vehicle.Sum(item => item.MedicalExpensesAmount)), 2);
                model.PassengerAccidentCoverAmount = Math.Round(Convert.ToDecimal(vehicle.Sum(item => item.PassengerAccidentCoverAmount)), 2);
                model.RoadsideAssistanceAmount = Math.Round(Convert.ToDecimal(vehicle.Sum(item => item.RoadsideAssistanceAmount)), 2);
                model.ExcessAmount = Math.Round(Convert.ToDecimal(vehicle.Sum(item => item.ExcessAmount)), 2);
                model.AmountPaid = 0.00m;
                model.MaxAmounttoPaid = Math.Round(Convert.ToDecimal(model.TotalPremium), 2);
                var vehiclewithminpremium = vehicle.OrderBy(x => x.Premium).FirstOrDefault();

                if (vehiclewithminpremium != null)
                {
                    model.MinAmounttoPaid = Math.Round(Convert.ToDecimal(vehiclewithminpremium.Premium + vehiclewithminpremium.StampDuty + vehiclewithminpremium.ZTSCLevy + (Convert.ToBoolean(vehiclewithminpremium.IncludeRadioLicenseCost) ? vehiclewithminpremium.RadioLicenseCost : 0.00m)), 2);
                }

                model.AmountPaid = Convert.ToDecimal(model.TotalPremium);
                model.BalancePaidDate = DateTime.Now;
                model.Notes = "";

                if (Session["PolicyData"] != null)
                {
                    var PolicyData = (PolicyDetail)Session["PolicyData"];
                    model.InvoiceNumber = PolicyData.PolicyNumber;
                }

                if (summarydetail != null)
                {
                    model.Id = summarydetail.Id;
                }

            }
            catch (Exception ex)
            {
              //  WriteLog(ex.Message);
                return View(model);
            }

            //   model.IceCashModel = 

            if (paymentError != "")
            {
                model.Error = "Error occurd during ecocash payment.";
                model.PaymentMethodId = (int)paymentMethod.ecocash;
            }


            return View(model);
        }

        public ActionResult ProductDetail()
        {
            var model = new PolicyDetailModel();
            var InsService = new InsurerService();
            model.CurrencyId = InsuranceContext.Currencies.All().FirstOrDefault().Id;
            model.PolicyStatusId = InsuranceContext.PolicyStatuses.All().FirstOrDefault().Id;
            model.BusinessSourceId = InsuranceContext.BusinessSources.All().FirstOrDefault().Id;
            //model.Products = InsuranceContext.Products.All().ToList();
            model.InsurerId = InsService.GetInsurers().FirstOrDefault().Id;
            var objList = InsuranceContext.PolicyDetails.All(orderBy: "Id desc").FirstOrDefault();
            if (objList != null)
            {
                string number = objList.PolicyNumber.Split('-')[0].Substring(4, objList.PolicyNumber.Length - 6);
                long pNumber = Convert.ToInt64(number.Substring(2, number.Length - 2)) + 1;
                string policyNumber = string.Empty;
                int length = 7;
                length = length - pNumber.ToString().Length;
                for (int i = 0; i < length; i++)
                {
                    policyNumber += "0";
                }
                policyNumber += pNumber;
                ViewBag.PolicyNumber = "GMCC" + DateTime.Now.Year.ToString().Substring(2, 2) + policyNumber + "-1";
                model.PolicyNumber = ViewBag.PolicyNumber;
            }
            else
            {
                ViewBag.PolicyNumber = ConfigurationManager.AppSettings["PolicyNumber"] + "-1";
                model.PolicyNumber = ViewBag.PolicyNumber;
            }

            model.BusinessSourceId = 3;

            Session["PolicyData"] = Mapper.Map<PolicyDetailModel, PolicyDetail>(model);

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Staff"))
                {
                    return RedirectToAction("RiskDetail", "ContactCentre");
                }
            }


            return RedirectToAction("RiskDetail");
        }


        [HttpPost]
        public async Task<ActionResult> SubmitPlan(SummaryDetailModel model, string btnSendQuatation = "")
        {
            SummaryDetailService servicedetail = new SummaryDetailService();
            try
            {
                if (model != null)
                {
                    //if (ModelState.IsValid && (model.AmountPaid >= model.MinAmounttoPaid && model.AmountPaid <= model.MaxAmounttoPaid))

                    int CustomerUniquId = 0;
                    if (User.IsInRole("Administrator"))
                    {
                        TempData["SucessMsg"] = "Admin can not create policy.";
                        return RedirectToAction("SummaryDetail");
                    }


                    TempData["ErroMsg"] = null;
                    if (User.IsInRole("Staff") && model.PaymentMethodId == 1 && btnSendQuatation == "")
                    {
                        //  ModelState.Remove("InvoiceNumber");
                        if (string.IsNullOrEmpty(model.InvoiceNumber))
                        {
                            TempData["ErroMsg"] = "Please enter invoice number.";
                            return RedirectToAction("SummaryDetail");
                        }
                    }


                    if (ModelState.IsValid)
                    {
                        Insurance.Service.ICEcashService ICEcashService = new Insurance.Service.ICEcashService();
                        List<RiskDetailModel> list = new List<RiskDetailModel>();
                        string PartnerToken = "";

                        #region update  TPIQuoteUpdate
                        var customerDetails = new Customer();

                        var policyDetils = new PolicyDetail();

                        var customerEmail = "";
                        var policyNum = "";
                        var InsuranceID = "";
                        var vichelDetails = new VehicleDetail();


                        if (model.Id != 0)
                        {
                            model.CustomSumarryDetilId = model.Id;
                        }

                        var summaryDetial = InsuranceContext.SummaryVehicleDetails.Single(where: $"SummaryDetailId = '" + model.CustomSumarryDetilId + "'");




                        if (summaryDetial != null && btnSendQuatation == "") // while user come from qutation email
                        {
                            if (model.CustomSumarryDetilId != 0 && btnSendQuatation == "") // cehck if request is comming from agent email
                            {
                                if (model.PaymentMethodId == 1)
                                    return RedirectToAction("SaveDetailList", new { id = model.CustomSumarryDetilId, invoiceNumber = model.InvoiceNumber });
                               else if (model.PaymentMethodId == (int)paymentMethod.ecocash)
                                {
                                    //return RedirectToAction("InitiatePaynowTransaction", "Paypal", new { id = model.CustomSumarryDetilId, TotalPremiumPaid = Convert.ToString(model.AmountPaid), PolicyNumber = policyNum, Email = customerEmail });
                                    TempData["PaymentMethodId"] = model.PaymentMethodId;
                                    // return RedirectToAction("makepayment", new { id = model.CustomSumarryDetilId, TotalPremiumPaid = Convert.ToString(model.AmountPaid) }); paynow
                                    return RedirectToAction("SaveDetailList", "Paypal", new { id = model.CustomSumarryDetilId, invoiceNumer = model.InvoiceNumber, Paymentid = model.PaymentMethodId.Value });
                                }
                                //else if (model.PaymentMethodId == 4)
                                //{
                                //    TempData["PaymentMethodId"] = model.PaymentMethodId;
                                //    return RedirectToAction("IceCashPayment", "Paypal", new { id = model.CustomSumarryDetilId, TotalPremiumPaid = Convert.ToString(model.AmountPaid) });
                                //}
                                else if (model.PaymentMethodId == (int)paymentMethod.Zimswitch)
                                {
                                    TempData["PaymentMethodId"] = model.PaymentMethodId;
                                    return RedirectToAction("IceCashPayment", "Paypal", new { id = model.CustomSumarryDetilId, amount = Convert.ToString(model.AmountPaid), Paymentid = model.PaymentMethodId.Value });
                                }                          
                                else
                                    return RedirectToAction("PaymentDetail", new { id = model.CustomSumarryDetilId, invoiceNumer = model.InvoiceNumber, Paymentid = model.PaymentMethodId.Value });
                            }
                        }


                        #endregion


                        #region Add All info to database

                        //var vehicle = (RiskDetailModel)Session["VehicleDetail"];
                        Session["SummaryDetailed"] = model;
                        string SummeryofReinsurance = "";
                        string SummeryofVehicleInsured = "";
                        bool userLoggedin = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
                        var customer = (CustomerModel)Session["CustomerDataModal"];



                        var role = "";

                        if (System.Web.HttpContext.Current.User.Identity.GetUserId() != null)
                        {
                            role = UserManager.GetRoles(System.Web.HttpContext.Current.User.Identity.GetUserId()).FirstOrDefault();

                        }

                        var userDetials = UserManager.FindByEmail(customer.EmailAddress);

                        if (userDetials == null)
                        {
                            customer.Id = 0;
                        }

                        //if user staff

                        if (role == "AgentStaff" || role == "Renewals" || role == "Administrator")
                        {
                            // check if email id exist in user table

                            var user = UserManager.FindByEmail(customer.EmailAddress);

                            // if exist - get customer id from xcustomer table and set customer.Id in Customer object
                            if (user != null && user.Id != null)
                            {

                                var customerDetials = InsuranceContext.Customers.Single(where: $"UserID = '" + user.Id + "'");

                                if (customerDetials != null)
                                {
                                    customer.Id = customerDetials.Id;

                                    CustomerUniquId = customerDetials.Id;


                                    // need to do work
                                    //if (btnSendQuatation != "" && model.Id != 0)
                                    //{
                                    //    var SummaryDetails = InsuranceContext.SummaryDetails.Single(where: $"CustomerId={customer.Id} and isQuotation = 'True'");
                                    //    if (SummaryDetails != null)
                                    //    {
                                    //        TempData["SucessMsg"] = customer.FirstName + " " + customer.LastName + " Quotation alredy exist, please edit existing.";
                                    //        return RedirectToAction("SummaryDetail");
                                    //    }
                                    //}


                                }

                            }
                        }


                        if (!userLoggedin)  // create new user without logged in
                        {
                            if (customer != null)
                            {
                                if (customer.Id == null || customer.Id == 0)
                                {
                                    decimal custId = 0;
                                    var user = new ApplicationUser { UserName = customer.EmailAddress, Email = customer.EmailAddress, PhoneNumber = customer.PhoneNumber };
                                    var result = await UserManager.CreateAsync(user, "Geninsure@123");
                                    if (result.Succeeded)
                                    {
                                        try
                                        {
                                            var roleresult = UserManager.AddToRole(user.Id, "Web Customer"); // for web user
                                        }
                                        catch (Exception ex)
                                        {
                                        }

                                        var objCustomer = InsuranceContext.Customers.All().OrderByDescending(x => x.Id).FirstOrDefault();
                                        if (objCustomer != null)
                                        {
                                            custId = objCustomer.CustomerId + 1;
                                        }
                                        else
                                        {
                                            custId = Convert.ToDecimal(ConfigurationManager.AppSettings["CustomerId"]);
                                        }

                                        customer.UserID = user.Id;
                                        customer.CustomerId = custId;
                                        var customerdata = Mapper.Map<CustomerModel, Customer>(customer);
                                        InsuranceContext.Customers.Insert(customerdata);
                                        customer.Id = customerdata.Id;
                                    }
                                }
                            }
                        }
                        else if (userLoggedin && userDetials == null) //  when user is logged in
                        {

                            if (customer.Id == null || customer.Id == 0)
                            {
                                decimal custId = 0;


                                var user = new ApplicationUser { UserName = customer.EmailAddress, Email = customer.EmailAddress, PhoneNumber = customer.PhoneNumber };
                                var result = await UserManager.CreateAsync(user, "Geninsure@123");
                                if (result.Succeeded)
                                {
                                    try
                                    {
                                        var roleresult = UserManager.AddToRole(user.Id, "Customer");
                                    }
                                    catch (Exception ex)
                                    {
                                    }

                                    var objCustomer = InsuranceContext.Customers.All().OrderByDescending(x => x.Id).FirstOrDefault();



                                    //Query
                                    if (objCustomer != null)
                                    {
                                        custId = objCustomer.CustomerId + 1;
                                    }
                                    else
                                    {
                                        custId = Convert.ToDecimal(ConfigurationManager.AppSettings["CustomerId"]);
                                    }

                                    customer.UserID = user.Id;
                                    customer.CustomerId = custId;
                                    var customerdata = Mapper.Map<CustomerModel, Customer>(customer);
                                    InsuranceContext.Customers.Insert(customerdata);
                                    customer.Id = customerdata.Id;
                                }
                            }
                        }
                        else if (userLoggedin && userDetials != null && customer.Id == 0) //  when user is logged in
                        {
                            decimal custId = 0;

                            var objCustomer = InsuranceContext.Customers.All().OrderByDescending(x => x.Id).FirstOrDefault();
                            //Query
                            if (objCustomer != null)
                            {
                                custId = objCustomer.CustomerId + 1;
                            }
                            else
                            {
                                custId = Convert.ToDecimal(ConfigurationManager.AppSettings["CustomerId"]);
                            }

                            customer.UserID = userDetials.Id;
                            customer.CustomerId = custId;
                            var customerdata = Mapper.Map<CustomerModel, Customer>(customer);
                            InsuranceContext.Customers.Insert(customerdata);
                            customer.Id = customerdata.Id;


                        }
                        else
                        {
                            //  var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                            var user = UserManager.FindByEmail(customer.EmailAddress);
                            //var objCustomer = InsuranceContext.Customers.Single(where: $"Userid=@0", parms: new object[] { User.Identity.GetUserId() });

                            if (user != null)
                            {
                                var number = user.PhoneNumber;
                                if (number != customer.PhoneNumber)
                                {
                                    user.PhoneNumber = customer.PhoneNumber;
                                   // UserManager.Update(user);  // 13_june
                                }
                                // customer.UserID = User.Identity.GetUserId().ToString();

                                var customerDetials = InsuranceContext.Customers.Single(where: $"UserID = '" + user.Id + "'");

                                if (customerDetials != null)
                                {
                                   // customer.UserID = user.Id;  // 13_june_2019
                                    customer.CustomerId = customerDetials.CustomerId;
                                    var customerdata = Mapper.Map<CustomerModel, Customer>(customer);

                                    if (customerdata.CustomerId == 0) // if exting record belong to 0
                                    {
                                        customerdata.CustomerId = customerdata.Id;
                                    }
                                 //   InsuranceContext.Customers.Update(customerdata); // 13_june_2019
                                }
                            }
                        }


                        var policy = (PolicyDetail)Session["PolicyData"];


                        // Genrate new policy number

                        if (policy != null && policy.Id == 0)
                        {
                            string policyNumber = string.Empty;

                            var objList = InsuranceContext.PolicyDetails.All(orderBy: "Id desc").FirstOrDefault();
                            if (objList != null)
                            {
                                string number = objList.PolicyNumber.Split('-')[0].Substring(4, objList.PolicyNumber.Length - 6);
                                long pNumber = Convert.ToInt64(number.Substring(2, number.Length - 2)) + 1;

                                int length = 7;
                                length = length - pNumber.ToString().Length;
                                for (int i = 0; i < length; i++)
                                {
                                    policyNumber += "0";
                                }
                                policyNumber += pNumber;
                                policy.PolicyNumber = "GMCC" + DateTime.Now.Year.ToString().Substring(2, 2) + policyNumber + "-1";

                            }
                        }
                        // end genrate policy number


                        if (policy != null)
                        {
                            if (policy.Id == null || policy.Id == 0)
                            {
                                policy.CustomerId = customer.Id;
                                policy.StartDate = null;
                                policy.EndDate = null;
                                policy.TransactionDate = null;
                                policy.RenewalDate = null;
                                policy.RenewalDate = null;
                                policy.StartDate = null;
                                policy.TransactionDate = null;
                                policy.CreatedBy = customer.Id;
                                policy.CreatedOn = DateTime.Now;
                                InsuranceContext.PolicyDetails.Insert(policy);

                                Session["PolicyData"] = policy;
                            }
                            else
                            {
                                PolicyDetail policydata = InsuranceContext.PolicyDetails.All(policy.Id.ToString()).FirstOrDefault();
                                policydata.BusinessSourceId = policy.BusinessSourceId;
                                policydata.CurrencyId = policy.CurrencyId;
                                // policydata.CustomerId = policy.CustomerId;
                                policydata.CustomerId = customer.Id;
                                policydata.EndDate = null;
                                policydata.InsurerId = policy.InsurerId;
                                policydata.IsActive = policy.IsActive;
                                policydata.PolicyName = policy.PolicyName;
                                policydata.PolicyNumber = policy.PolicyNumber;
                                policydata.PolicyStatusId = policy.PolicyStatusId;
                                policydata.RenewalDate = null;
                                policydata.StartDate = null;
                                policydata.TransactionDate = null;
                                policy.ModifiedBy = customer.Id;
                                policy.ModifiedOn = DateTime.Now;
                                InsuranceContext.PolicyDetails.Update(policydata);
                            }

                        }
                        var Id = 0;
                        var listReinsuranceTransaction = new List<ReinsuranceTransaction>();
                        var vehicle = (List<RiskDetailModel>)Session["VehicleDetails"];


                        if (vehicle != null && vehicle.Count > 0)
                        {
                            foreach (var item in vehicle.ToList())
                            {
                                var _item = item;

                                //List<RiskDetailModel> objVehicles = new List<RiskDetailModel>();
                                ////objVehicles.Add(new RiskDetailModel { RegistrationNo = regNo });
                                //objVehicles.Add(new RiskDetailModel { RegistrationNo = _item.RegistrationNo, PaymentTermId = Convert.ToInt32(_item.PaymentTermId) });
                                //var  tokenObject = (ICEcashTokenResponse)Session["ICEcashToken"];
                                //ResultRootObject quoteresponse = ICEcashService.checkVehicleExists(objVehicles, tokenObject.Response.PartnerToken, tokenObject.PartnerReference);

                                //if (quoteresponse.Response.Result == 0)
                                //{
                                //    response.message = quoteresponse.Response.Quotes[0].Message;
                                //}
                                //else
                                //{
                                //    response.Data = quoteresponse;
                                //}


                                var vehicelDetails = InsuranceContext.VehicleDetails.Single(where: $"policyid= '{policy.Id}' and RegistrationNo= '{_item.RegistrationNo}'");

                                if (vehicelDetails != null)
                                {
                                    item.Id = vehicelDetails.Id;
                                }


                                if (item.Id == null || item.Id == 0)
                                {
                                    var service = new RiskDetailService();
                                    _item.CustomerId = customer.Id;
                                    _item.PolicyId = policy.Id;
                                    //   _item.InsuranceId = model.InsuranceId;
                                    //if (model.AmountPaid < model.TotalPremium)
                                    //{
                                    //    _item.BalanceAmount = (_item.Premium + _item.ZTSCLevy + _item.StampDuty + (_item.IncludeRadioLicenseCost ? _item.RadioLicenseCost : 0.00m) - _item.Discount) - (model.AmountPaid / vehicle.Count);
                                    //}

                                    _item.Id = service.AddVehicleInformation(_item);
                                    var vehicles = (List<RiskDetailModel>)Session["VehicleDetails"];
                                    vehicles[Convert.ToInt32(_item.NoOfCarsCovered) - 1] = _item;
                                    Session["VehicleDetails"] = vehicles;


                                    // Delivery Address Save
                                    var LicenseAddress = new LicenceDiskDeliveryAddress();
                                    LicenseAddress.Address1 = _item.LicenseAddress1;
                                    LicenseAddress.Address2 = _item.LicenseAddress2;
                                    LicenseAddress.City = _item.LicenseCity;
                                    LicenseAddress.VehicleId = _item.Id;
                                    LicenseAddress.CreatedBy = customer.Id;
                                    LicenseAddress.CreatedOn = DateTime.Now;
                                    LicenseAddress.ModifiedBy = customer.Id;
                                    LicenseAddress.ModifiedOn = DateTime.Now;

                                    InsuranceContext.LicenceDiskDeliveryAddresses.Insert(LicenseAddress);


                                    ///Licence Ticket
                                    if (_item.IsLicenseDiskNeeded)
                                    {

                                        var LicenceTicket = new LicenceTicket();
                                        var Licence = InsuranceContext.LicenceTickets.All(orderBy: "Id desc").FirstOrDefault();

                                        if (Licence != null)
                                        {


                                            string number = Licence.TicketNo.Substring(3);

                                            long tNumber = Convert.ToInt64(number) + 1;
                                            string TicketNo = string.Empty;
                                            int length = 6;
                                            length = length - tNumber.ToString().Length;

                                            for (int i = 0; i < length; i++)
                                            {
                                                TicketNo += "0";
                                            }
                                            TicketNo += tNumber;
                                            var ticketnumber = "GEN" + TicketNo;

                                            LicenceTicket.TicketNo = ticketnumber;
                                        }
                                        else
                                        {
                                            var TicketNo = ConfigurationManager.AppSettings["TicketNo"];

                                            LicenceTicket.TicketNo = TicketNo;


                                        }

                                        LicenceTicket.VehicleId = _item.Id;
                                        LicenceTicket.CloseComments = "";
                                        LicenceTicket.ReopenComments = "";
                                        LicenceTicket.DeliveredTo = "";
                                        LicenceTicket.CreatedDate = DateTime.Now;
                                        LicenceTicket.CreatedBy = customer.Id;
                                        LicenceTicket.IsClosed = false;
                                        LicenceTicket.PolicyNumber = policy.PolicyNumber;

                                        InsuranceContext.LicenceTickets.Insert(LicenceTicket);
                                    }

                                    ///Reinsurance                      

                                    var ReinsuranceCases = InsuranceContext.Reinsurances.All(where: $"Type='Reinsurance'").ToList();
                                    var ownRetention = InsuranceContext.Reinsurances.All().Where(x => x.TreatyCode == "OR001").Select(x => x.MaxTreatyCapacity).SingleOrDefault();
                                    var ReinsuranceCase = new Reinsurance();

                                    foreach (var Reinsurance in ReinsuranceCases)
                                    {
                                        if (Reinsurance.MinTreatyCapacity <= item.SumInsured && item.SumInsured <= Reinsurance.MaxTreatyCapacity)
                                        {
                                            ReinsuranceCase = Reinsurance;
                                            break;
                                        }
                                    }

                                    if (ReinsuranceCase != null && ReinsuranceCase.MaxTreatyCapacity != null)
                                    {
                                        var basicPremium = item.Premium;
                                        var ReinsuranceBroker = InsuranceContext.ReinsuranceBrokers.Single(where: $"ReinsuranceBrokerCode='{ReinsuranceCase.ReinsuranceBrokerCode}'");
                                        var AutoFacSumInsured = 0.00m;
                                        var AutoFacPremium = 0.00m;
                                        var FacSumInsured = 0.00m;
                                        var FacPremium = 0.00m;

                                        if (ReinsuranceCase.MinTreatyCapacity > 200000)
                                        {
                                            var autofaccase = ReinsuranceCases.FirstOrDefault();
                                            var autofacSumInsured = autofaccase.MaxTreatyCapacity - ownRetention;
                                            var autofacReinsuranceBroker = InsuranceContext.ReinsuranceBrokers.Single(where: $"ReinsuranceBrokerCode='{autofaccase.ReinsuranceBrokerCode}'");

                                            var _reinsurance = new ReinsuranceTransaction();
                                            _reinsurance.ReinsuranceAmount = autofacSumInsured;
                                            AutoFacSumInsured = Convert.ToDecimal(_reinsurance.ReinsuranceAmount);
                                            _reinsurance.ReinsurancePremium = Math.Round(Convert.ToDecimal((_reinsurance.ReinsuranceAmount / item.SumInsured) * basicPremium), 2);
                                            AutoFacPremium = Convert.ToDecimal(_reinsurance.ReinsurancePremium);
                                            _reinsurance.ReinsuranceCommissionPercentage = Convert.ToDecimal(autofacReinsuranceBroker.Commission);
                                            _reinsurance.ReinsuranceCommission = Math.Round(Convert.ToDecimal((_reinsurance.ReinsurancePremium * _reinsurance.ReinsuranceCommissionPercentage) / 100), 2);
                                            _reinsurance.VehicleId = item.Id;
                                            _reinsurance.ReinsuranceBrokerId = autofacReinsuranceBroker.Id;
                                            _reinsurance.TreatyName = autofaccase.TreatyName;
                                            _reinsurance.TreatyCode = autofaccase.TreatyCode;
                                            _reinsurance.CreatedOn = DateTime.Now;
                                            _reinsurance.CreatedBy = customer.Id;

                                            InsuranceContext.ReinsuranceTransactions.Insert(_reinsurance);

                                            SummeryofReinsurance += "<tr><td>" + Convert.ToString(_reinsurance.Id) + "</td><td>" + ReinsuranceCase.TreatyCode + "</td><td>" + ReinsuranceCase.TreatyName + "</td><td>" + Convert.ToString(_reinsurance.ReinsuranceAmount) + "</td><td>" + Convert.ToString(ReinsuranceBroker.ReinsuranceBrokerName) + "</td><td>" + Convert.ToString(Math.Round(Convert.ToDecimal(_reinsurance.ReinsurancePremium), 2)) + "</td><td>" + Convert.ToString(ReinsuranceBroker.Commission) + "</td></tr>";

                                            listReinsuranceTransaction.Add(_reinsurance);

                                            var __reinsurance = new ReinsuranceTransaction();
                                            __reinsurance.ReinsuranceAmount = _item.SumInsured - ownRetention - autofacSumInsured;
                                            FacSumInsured = Convert.ToDecimal(__reinsurance.ReinsuranceAmount);
                                            __reinsurance.ReinsurancePremium = Math.Round(Convert.ToDecimal((__reinsurance.ReinsuranceAmount / item.SumInsured) * basicPremium), 2);
                                            FacPremium = Convert.ToDecimal(__reinsurance.ReinsurancePremium);
                                            __reinsurance.ReinsuranceCommissionPercentage = Convert.ToDecimal(ReinsuranceBroker.Commission);
                                            __reinsurance.ReinsuranceCommission = Math.Round(Convert.ToDecimal((__reinsurance.ReinsurancePremium * __reinsurance.ReinsuranceCommissionPercentage) / 100), 2);
                                            __reinsurance.VehicleId = item.Id;
                                            __reinsurance.ReinsuranceBrokerId = ReinsuranceBroker.Id;
                                            __reinsurance.TreatyName = ReinsuranceCase.TreatyName;
                                            __reinsurance.TreatyCode = ReinsuranceCase.TreatyCode;
                                            __reinsurance.CreatedOn = DateTime.Now;
                                            __reinsurance.CreatedBy = customer.Id;

                                            InsuranceContext.ReinsuranceTransactions.Insert(__reinsurance);

                                            //SummeryofReinsurance += "<tr><td>" + Convert.ToString(__reinsurance.Id) + "</td><td>" + ReinsuranceCase.TreatyCode + "</td><td>" + ReinsuranceCase.TreatyName + "</td><td>" + Convert.ToString(__reinsurance.ReinsuranceAmount) + "</td><td>" + Convert.ToString(ReinsuranceBroker.ReinsuranceBrokerName) + "</td><td>" + Convert.ToString(Math.Round(Convert.ToDecimal(__reinsurance.ReinsurancePremium), 2)) + "</td><td>" + Convert.ToString(ReinsuranceBroker.Commission) + "</td></tr>";

                                            listReinsuranceTransaction.Add(__reinsurance);
                                        }
                                        else
                                        {

                                            var reinsurance = new ReinsuranceTransaction();
                                            reinsurance.ReinsuranceAmount = _item.SumInsured - ownRetention;
                                            AutoFacSumInsured = Convert.ToDecimal(reinsurance.ReinsuranceAmount);
                                            reinsurance.ReinsurancePremium = Math.Round(Convert.ToDecimal((reinsurance.ReinsuranceAmount / item.SumInsured) * basicPremium), 2);
                                            AutoFacPremium = Convert.ToDecimal(reinsurance.ReinsurancePremium);
                                            reinsurance.ReinsuranceCommissionPercentage = Convert.ToDecimal(ReinsuranceBroker.Commission);
                                            reinsurance.ReinsuranceCommission = Math.Round(Convert.ToDecimal((reinsurance.ReinsurancePremium * reinsurance.ReinsuranceCommissionPercentage) / 100), 2);
                                            reinsurance.VehicleId = item.Id;
                                            reinsurance.ReinsuranceBrokerId = ReinsuranceBroker.Id;
                                            reinsurance.TreatyName = ReinsuranceCase.TreatyName;
                                            reinsurance.TreatyCode = ReinsuranceCase.TreatyCode;
                                            reinsurance.CreatedOn = DateTime.Now;
                                            reinsurance.CreatedBy = customer.Id;

                                            InsuranceContext.ReinsuranceTransactions.Insert(reinsurance);

                                            //SummeryofReinsurance += "<tr><td>" + Convert.ToString(reinsurance.Id) + "</td><td>" + ReinsuranceCase.TreatyCode + "</td><td>" + ReinsuranceCase.TreatyName + "</td><td>" + Convert.ToString(reinsurance.ReinsuranceAmount) + "</td><td>" + Convert.ToString(ReinsuranceBroker.ReinsuranceBrokerName) + "</td><td>" + Convert.ToString(Math.Round(Convert.ToDecimal(reinsurance.ReinsurancePremium), 2)) + "</td><td>" + Convert.ToString(ReinsuranceBroker.Commission) + "</td></tr>";

                                            listReinsuranceTransaction.Add(reinsurance);
                                        }


                                        Insurance.Service.VehicleService obj = new Insurance.Service.VehicleService();
                                        VehicleModel vehiclemodel = InsuranceContext.VehicleModels.Single(where: $"ModelCode='{item.ModelId}'");
                                        VehicleMake vehiclemake = InsuranceContext.VehicleMakes.Single(where: $" MakeCode='{item.MakeId}'");

                                        string vehicledescription = vehiclemodel.ModelDescription + " / " + vehiclemake.MakeDescription;

                                        // SummeryofVehicleInsured += "<tr><td>" + vehicledescription + "</td><td>" + Convert.ToString(item.SumInsured) + "</td><td>" + Convert.ToString(item.Premium) + "</td><td>" + AutoFacSumInsured.ToString() + "</td><td>" + AutoFacPremium.ToString() + "</td><td>" + FacSumInsured.ToString() + "</td><td>" + FacPremium.ToString() + "</td></tr>";

                                        SummeryofVehicleInsured += "<tr><td style='padding:7px 10px; font-size:14px'><font size='2'>" + vehicledescription + "</font></td><td style='padding:7px 10px; font-size:14px'><font size='2'>" + Convert.ToString(item.SumInsured) + " </font></td><td style='padding:7px 10px; font-size:14px'><font size='2'>" + Convert.ToString(item.Premium) + "</font></td><td style='padding:7px 10px; font-size:14px'><font size='2'>" + AutoFacSumInsured.ToString() + "</font></td><td style='padding:7px 10px; font-size:14px'><font size='2'>" + AutoFacPremium.ToString() + "</ font ></td><td style='padding:7px 10px; font-size:14px'><font size='2'>" + FacSumInsured.ToString() + "</font></td><td style='padding:7px 10px; font-size:14px'><font size='2'>" + FacPremium.ToString() + "</font></td></tr>";



                                    }

                                }
                                else
                                {
                                    VehicleDetail Vehicledata = InsuranceContext.VehicleDetails.All(item.Id.ToString()).FirstOrDefault();
                                    Vehicledata.AgentCommissionId = item.AgentCommissionId;
                                    Vehicledata.ChasisNumber = item.ChasisNumber;
                                    Vehicledata.CoverEndDate = item.CoverEndDate;
                                    Vehicledata.CoverNoteNo = item.CoverNoteNo;
                                    Vehicledata.CoverStartDate = item.CoverStartDate;
                                    Vehicledata.CoverTypeId = item.CoverTypeId;
                                    Vehicledata.CubicCapacity = item.CubicCapacity;
                                    Vehicledata.EngineNumber = item.EngineNumber;
                                    Vehicledata.Excess = item.Excess;
                                    Vehicledata.ExcessType = item.ExcessType;
                                    Vehicledata.MakeId = item.MakeId;
                                    Vehicledata.ModelId = item.ModelId;
                                    Vehicledata.NoOfCarsCovered = item.NoOfCarsCovered;
                                    Vehicledata.OptionalCovers = item.OptionalCovers;
                                    Vehicledata.PolicyId = item.PolicyId;
                                    Vehicledata.Premium = item.Premium;
                                    Vehicledata.RadioLicenseCost = (item.IsLicenseDiskNeeded ? item.RadioLicenseCost : 0.00m);
                                    Vehicledata.Rate = item.Rate;
                                    Vehicledata.RegistrationNo = item.RegistrationNo;
                                    Vehicledata.StampDuty = item.StampDuty;
                                    Vehicledata.SumInsured = item.SumInsured;
                                    Vehicledata.VehicleColor = item.VehicleColor;
                                    Vehicledata.VehicleUsage = item.VehicleUsage;
                                    Vehicledata.VehicleYear = item.VehicleYear;
                                    Vehicledata.ZTSCLevy = item.ZTSCLevy;
                                    Vehicledata.Addthirdparty = item.Addthirdparty;
                                    Vehicledata.AddThirdPartyAmount = item.AddThirdPartyAmount;
                                    Vehicledata.PassengerAccidentCover = item.PassengerAccidentCover;
                                    Vehicledata.ExcessBuyBack = item.ExcessBuyBack;
                                    Vehicledata.RoadsideAssistance = item.RoadsideAssistance;

                                    // 006_feb
                                    Vehicledata.RoadsideAssistanceAmount = item.RoadsideAssistanceAmount;
                                    Vehicledata.MedicalExpensesAmount = item.MedicalExpensesAmount;



                                    Vehicledata.MedicalExpenses = item.MedicalExpenses;
                                    Vehicledata.NumberofPersons = item.NumberofPersons;
                                    Vehicledata.IsLicenseDiskNeeded = item.IsLicenseDiskNeeded;
                                    Vehicledata.AnnualRiskPremium = item.AnnualRiskPremium;
                                    Vehicledata.TermlyRiskPremium = item.TermlyRiskPremium;
                                    Vehicledata.QuaterlyRiskPremium = item.QuaterlyRiskPremium;
                                    Vehicledata.TransactionDate = DateTime.Now;


                                    if (Vehicledata.ExcessBuyBack == true)
                                    {
                                        Vehicledata.ExcessBuyBackAmount = item.ExcessBuyBackAmount;
                                    }

                                    if (Vehicledata.PassengerAccidentCover == true)
                                    {
                                        Vehicledata.PassengerAccidentCoverAmount = item.PassengerAccidentCoverAmount;
                                    }
                                    if (Vehicledata.ExcessBuyBack == true)
                                    {
                                        Vehicledata.ExcessBuyBackAmount = item.ExcessBuyBackAmount;
                                    }

                                    if (Vehicledata.PassengerAccidentCover == true)
                                    {
                                        Vehicledata.PassengerAccidentCoverAmount = item.PassengerAccidentCoverAmount;
                                    }





                                    Vehicledata.CustomerId = customer.Id;
                                    // Vehicledata.InsuranceId = model.InsuranceId;

                                    InsuranceContext.VehicleDetails.Update(Vehicledata);
                                    var _summary = (SummaryDetailModel)Session["SummaryDetailed"];


                                    var ReinsuranceCases = InsuranceContext.Reinsurances.All(where: $"Type='Reinsurance'").ToList();
                                    var ownRetention = InsuranceContext.Reinsurances.All().Where(x => x.TreatyCode == "OR001").Select(x => x.MaxTreatyCapacity).SingleOrDefault();
                                    var ReinsuranceCase = new Reinsurance();

                                    foreach (var Reinsurance in ReinsuranceCases)
                                    {
                                        if (Reinsurance.MinTreatyCapacity <= item.SumInsured && item.SumInsured <= Reinsurance.MaxTreatyCapacity)
                                        {
                                            ReinsuranceCase = Reinsurance;
                                            break;
                                        }
                                    }

                                    if (ReinsuranceCase != null && ReinsuranceCase.MaxTreatyCapacity != null)
                                    {
                                        var ReinsuranceBroker = InsuranceContext.ReinsuranceBrokers.Single(where: $"ReinsuranceBrokerCode='{ReinsuranceCase.ReinsuranceBrokerCode}'");

                                        var summaryid = _summary.Id;
                                        var vehicleid = item.Id;
                                        var ReinsuranceTransactions = InsuranceContext.ReinsuranceTransactions.Single(where: $"SummaryDetailId={_summary.Id} and VehicleId={item.Id}");
                                        //var _reinsurance = new ReinsuranceTransaction();
                                        ReinsuranceTransactions.ReinsuranceAmount = _item.SumInsured - ownRetention;
                                        ReinsuranceTransactions.ReinsurancePremium = ((ReinsuranceTransactions.ReinsuranceAmount / item.SumInsured) * item.Premium);
                                        ReinsuranceTransactions.ReinsuranceCommissionPercentage = Convert.ToDecimal(ReinsuranceBroker.Commission);
                                        ReinsuranceTransactions.ReinsuranceCommission = ((ReinsuranceTransactions.ReinsurancePremium * ReinsuranceTransactions.ReinsuranceCommissionPercentage) / 100);//Convert.ToDecimal(defaultReInsureanceBroker.Commission);
                                        ReinsuranceTransactions.ReinsuranceBrokerId = ReinsuranceBroker.Id;

                                        InsuranceContext.ReinsuranceTransactions.Update(ReinsuranceTransactions);
                                    }
                                    else
                                    {
                                        var ReinsuranceTransactions = InsuranceContext.ReinsuranceTransactions.Single(where: $"SummaryDetailId={_summary.Id} and VehicleId={item.Id}");
                                        if (ReinsuranceTransactions != null)
                                        {
                                            InsuranceContext.ReinsuranceTransactions.Delete(ReinsuranceTransactions);
                                        }
                                    }

                                }
                            }
                        }

                        var summary = (SummaryDetailModel)Session["SummaryDetailed"];
                        var DbEntry = Mapper.Map<SummaryDetailModel, SummaryDetail>(model);


                        if (summary != null)
                        {
                            if (summary.Id == 0)
                            {
                                if (Session["VehicleDetails"] != null) // forcelly check because in some case summary details id is comming 0
                                {
                                    var vehicalDetailsForSummary = (List<RiskDetailModel>)Session["VehicleDetails"];
                                    if (vehicalDetailsForSummary[0].Id != 0)
                                    {
                                        var SummaryVehicalDetails = InsuranceContext.SummaryVehicleDetails.All(where: $"VehicleDetailsId={vehicalDetailsForSummary[0].Id}").ToList();

                                        if (SummaryVehicalDetails.Count() > 0)
                                        {
                                            summary.Id = SummaryVehicalDetails[0].SummaryDetailId;
                                        }
                                    }
                                }
                            }

                            if (summary.Id == null || summary.Id == 0)
                            {
                                //DbEntry.PaymentTermId = Convert.ToInt32(Session["policytermid"]);
                                //DbEntry.VehicleDetailId = vehicle[0].Id;
                                //  bool _userLoggedin = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;



                                // DbEntry.CustomerId = vehicle[0].CustomerId;
                                DbEntry.CustomerId = customer.Id;

                                bool _userLoggedin = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;


                                if (_userLoggedin)
                                {
                                    var _User = UserManager.FindById(User.Identity.GetUserId().ToString());
                                    var _customerData = InsuranceContext.Customers.All(where: $"UserId ='{_User.Id}'").FirstOrDefault();

                                    if (_customerData != null)
                                    {
                                        DbEntry.CreatedBy = _customerData.Id;
                                        DbEntry.AgentId = GetAgentId(_customerData.CreatedBy);
                                    }
                                }


                                DbEntry.CreatedOn = DateTime.Now;
                                if (DbEntry.BalancePaidDate.Value.Year == 0001)
                                {
                                    DbEntry.BalancePaidDate = DateTime.Now;
                                }
                                if (DbEntry.Notes == null)
                                {
                                    DbEntry.Notes = "";
                                }

                                if (!string.IsNullOrEmpty(btnSendQuatation))
                                {
                                    DbEntry.isQuotation = true;
                                }

                                DbEntry.ModuleName = _AgentModule;


                                InsuranceContext.SummaryDetails.Insert(DbEntry);
                                model.Id = DbEntry.Id;
                                Session["SummaryDetailed"] = model;
                            }
                            else
                            {
                                // SummaryDetail summarydata = InsuranceContext.SummaryDetails.All(summary.Id.ToString()).FirstOrDefault(); // on 05-oct for updatig qutation

                                var summarydata = Mapper.Map<SummaryDetailModel, SummaryDetail>(model);
                                summarydata.Id = summary.Id;
                                summarydata.CreatedOn = DateTime.Now;

                                if (!string.IsNullOrEmpty(btnSendQuatation))
                                {
                                    summarydata.isQuotation = true;
                                }


                                //summarydata.PaymentTermId = Convert.ToInt32(Session["policytermid"]);
                                //summarydata.VehicleDetailId = vehicle[0].Id;


                                bool _userLoggedin = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
                                if (_userLoggedin)
                                {
                                    var _User = UserManager.FindById(User.Identity.GetUserId().ToString());
                                    var _customerData = InsuranceContext.Customers.All(where: $"UserId ='{_User.Id}'").FirstOrDefault();

                                    if (_customerData != null)
                                    {
                                        summarydata.CreatedBy = _customerData.Id;
                                        summarydata.AgentId= GetAgentId(_customerData.CreatedBy);

                                    }
                                }


                                summarydata.ModifiedBy = customer.Id;
                                summarydata.ModifiedOn = DateTime.Now;
                                if (summarydata.BalancePaidDate.Value.Year == 0001)
                                {
                                    summarydata.BalancePaidDate = DateTime.Now;
                                }
                                if (DbEntry.Notes == null)
                                {
                                    summarydata.Notes = "";
                                }
                                //summarydata.CustomerId = vehicle[0].CustomerId;

                                summarydata.CustomerId = customer.Id;

                                InsuranceContext.SummaryDetails.Update(summarydata);
                                model.Id = summarydata.Id;
                            }



                            if (listReinsuranceTransaction != null && listReinsuranceTransaction.Count > 0)
                            {
                                foreach (var item in listReinsuranceTransaction)
                                {
                                    var InsTransac = InsuranceContext.ReinsuranceTransactions.Single(item.Id);
                                    InsTransac.SummaryDetailId = summary.Id;
                                    InsuranceContext.ReinsuranceTransactions.Update(InsTransac);
                                }
                            }

                        }



                        if (vehicle != null && vehicle.Count > 0 && summary.Id != null && summary.Id > 0)
                        {
                            var SummaryDetails = InsuranceContext.SummaryVehicleDetails.All(where: $"SummaryDetailId={summary.Id}").ToList();

                            if (SummaryDetails != null && SummaryDetails.Count > 0)
                            {
                                foreach (var item in SummaryDetails)
                                {
                                    InsuranceContext.SummaryVehicleDetails.Delete(item);
                                }
                            }

                            foreach (var item in vehicle.ToList())
                            {

                                try
                                {
                                    var summarydetails = new SummaryVehicleDetail();
                                    summarydetails.SummaryDetailId = summary.Id;
                                    summarydetails.VehicleDetailsId = item.Id;
                                    summarydetails.CreatedBy = customer.Id;
                                    summarydetails.CreatedOn = DateTime.Now;
                                    InsuranceContext.SummaryVehicleDetails.Insert(summarydetails);
                                }
                                catch (Exception ex)
                                {
                                    Insurance.Service.EmailService log = new Insurance.Service.EmailService();
                                    log.WriteLog("exception during insert vehicel :" + ex.Message);

                                }

                            }

                            MiscellaneousService.UpdateBalanceForVehicles(summary.AmountPaid, summary.Id, Convert.ToDecimal(summary.TotalPremium), false);

                        }

                        if (listReinsuranceTransaction != null && listReinsuranceTransaction.Count > 0)
                        {
                            string filepath = System.Configuration.ConfigurationManager.AppSettings["urlPath"];
                            int _vehicleId = 0;
                            int count = 0;
                            bool MailSent = false;
                            foreach (var item in listReinsuranceTransaction)
                            {

                                count++;
                                if (_vehicleId == 0)
                                {
                                    SummeryofReinsurance = "<tr><td>" + Convert.ToString(item.Id) + "</td><td>" + item.TreatyCode + "</td><td>" + item.TreatyName + "</td><td>" + Convert.ToString(item.ReinsuranceAmount) + "</td><td>" + MiscellaneousService.GetReinsuranceBrokerNamebybrokerid(item.ReinsuranceBrokerId) + "</td><td>" + Convert.ToString(Math.Round(Convert.ToDecimal(item.ReinsurancePremium), 2)) + "</td><td>" + Convert.ToString(item.ReinsuranceCommissionPercentage) + "%</td></tr>";
                                    _vehicleId = item.VehicleId;
                                    MailSent = false;
                                }
                                else
                                {
                                    if (_vehicleId == item.VehicleId)
                                    {
                                        SummeryofReinsurance += "<tr><td>" + Convert.ToString(item.Id) + "</td><td>" + item.TreatyCode + "</td><td>" + item.TreatyName + "</td><td>" + Convert.ToString(item.ReinsuranceAmount) + "</td><td>" + MiscellaneousService.GetReinsuranceBrokerNamebybrokerid(item.ReinsuranceBrokerId) + "</td><td>" + Convert.ToString(Math.Round(Convert.ToDecimal(item.ReinsurancePremium), 2)) + "</td><td>" + Convert.ToString(item.ReinsuranceCommissionPercentage) + "%</td></tr>";
                                        var user = UserManager.FindById(customer.UserID);
                                        Insurance.Service.EmailService objEmailService = new Insurance.Service.EmailService();
                                        var ePaymentTermData = from ePaymentTerm e in Enum.GetValues(typeof(ePaymentTerm)) select new { ID = (int)e, Name = e.ToString() };
                                        var paymentTerm = ePaymentTermData.FirstOrDefault(p => p.ID == summary.PaymentTermId);
                                        string SeheduleMotorPath = "/Views/Shared/EmaiTemplates/Reinsurance_Admin.cshtml";
                                        string MotorBody = System.IO.File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath(SeheduleMotorPath));
                                        var Body = MotorBody.Replace("##PolicyNo##", policy.PolicyNumber).Replace("##path##", filepath).Replace("##Cellnumber##", user.PhoneNumber)
                                            .Replace("##FirstName##", customer.FirstName).Replace("##LastName##", customer.LastName)
                                            .Replace("##SummeryofVehicleInsured##", SummeryofVehicleInsured);

                                        var attachementPath = MiscellaneousService.EmailPdf(Body, policy.CustomerId, policy.PolicyNumber, "Reinsurance Case");


                                        List<string> attachements = new List<string>();
                                        attachements.Add(attachementPath);

                                        objEmailService.SendEmail(ZimnatEmail, "", "", "Reinsurance Case: " + policy.PolicyNumber.ToString(), Body, attachements);
                                        MailSent = true;
                                    }
                                    else
                                    {
                                        SummeryofReinsurance = "<tr><td>" + Convert.ToString(item.Id) + "</td><td>" + item.TreatyCode + "</td><td>" + item.TreatyName + "</td><td>" + Convert.ToString(item.ReinsuranceAmount) + "</td><td>" + MiscellaneousService.GetReinsuranceBrokerNamebybrokerid(item.ReinsuranceBrokerId) + "</td><td>" + Convert.ToString(Math.Round(Convert.ToDecimal(item.ReinsurancePremium), 2)) + "</td><td>" + Convert.ToString(item.ReinsuranceCommissionPercentage) + "%</td></tr>";
                                        MailSent = false;
                                    }
                                    _vehicleId = item.VehicleId;
                                }


                                if (count == listReinsuranceTransaction.Count && !MailSent)
                                {
                                    var user = UserManager.FindById(customer.UserID);
                                    Insurance.Service.EmailService objEmailService = new Insurance.Service.EmailService();
                                    var ePaymentTermData = from ePaymentTerm e in Enum.GetValues(typeof(ePaymentTerm)) select new { ID = (int)e, Name = e.ToString() };
                                    var paymentTerm = ePaymentTermData.FirstOrDefault(p => p.ID == summary.PaymentTermId);
                                    string SeheduleMotorPath = "/Views/Shared/EmaiTemplates/Reinsurance_Admin.cshtml";
                                    string MotorBody = System.IO.File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath(SeheduleMotorPath));
                                    var Body = MotorBody.Replace("##PolicyNo##", policy.PolicyNumber).Replace("##paath##", filepath).Replace("##Cellnumber##", user.PhoneNumber).Replace("##FirstName##", customer.FirstName).Replace("##LastName##", customer.LastName).Replace("##SummeryofVehicleInsured##", SummeryofVehicleInsured);

                                    var attacehMentFilePath = MiscellaneousService.EmailPdf(Body, policy.CustomerId, policy.PolicyNumber, "Reinsurance Case");

                                    List<string> _attachements = new List<string>();
                                    _attachements.Add(attacehMentFilePath);
                                    objEmailService.SendEmail(ZimnatEmail, "", "", "Reinsurance Case: " + policy.PolicyNumber.ToString(), Body, _attachements);
                                    //MiscellaneousService.ScheduleMotorPdf(Body, policy.CustomerId, policy.PolicyNumber, "Reinsurance Case- " + policy.PolicyNumber.ToString(), item.VehicleId);
                                }


                            }
                        }

                        #endregion

                        #region Quotation Email
                        if (!string.IsNullOrEmpty(btnSendQuatation))
                        {
                            List<VehicleDetail> ListOfVehicles = new List<VehicleDetail>();
                            var SummaryVehicleDetails = InsuranceContext.SummaryVehicleDetails.All(where: $"SummaryDetailId={model.Id}").ToList();
                            foreach (var itemSummaryVehicleDetails in SummaryVehicleDetails)
                            {
                                var itemVehicle = InsuranceContext.VehicleDetails.Single(itemSummaryVehicleDetails.VehicleDetailsId);
                                ListOfVehicles.Add(itemVehicle);
                            }



                            var currencylist = servicedetail.GetAllCurrency();
                            string CurrencyName = "";



                            //List<VehicleDetail> ListOfVehicles = new List<VehicleDetail>();
                            string Summeryofcover = "";
                            var RoadsideAssistanceAmount = 0.00m;
                            var MedicalExpensesAmount = 0.00m;
                            var ExcessBuyBackAmount = 0.00m;
                            var PassengerAccidentCoverAmount = 0.00m;
                            var ExcessAmount = 0.00m;

                            var ePaymentTermData = from ePaymentTerm e in Enum.GetValues(typeof(ePaymentTerm)) select new { ID = (int)e, Name = e.ToString() };


                            foreach (var item in ListOfVehicles)
                            {
                                Insurance.Service.VehicleService obj = new Insurance.Service.VehicleService();
                                VehicleModel modell = InsuranceContext.VehicleModels.Single(where: $"ModelCode='{item.ModelId}'");
                                VehicleMake make = InsuranceContext.VehicleMakes.Single(where: $" MakeCode='{item.MakeId}'");

                                string vehicledescription = modell.ModelDescription + " / " + make.MakeDescription;

                                RoadsideAssistanceAmount = RoadsideAssistanceAmount + Convert.ToDecimal(item.RoadsideAssistanceAmount);
                                MedicalExpensesAmount = MedicalExpensesAmount + Convert.ToDecimal(item.MedicalExpensesAmount);
                                ExcessBuyBackAmount = ExcessBuyBackAmount + Convert.ToDecimal(item.ExcessBuyBackAmount);
                                PassengerAccidentCoverAmount = PassengerAccidentCoverAmount + Convert.ToDecimal(item.PassengerAccidentCoverAmount);
                                ExcessAmount = ExcessAmount + Convert.ToDecimal(item.ExcessAmount);


                                string converType = "";

                                if (item.CoverTypeId == 1)
                                {
                                    converType = eCoverType.ThirdParty.ToString();
                                }
                                if (item.CoverTypeId == 2)
                                {
                                    converType = eCoverType.FullThirdParty.ToString();
                                }

                                if (item.CoverTypeId == 4)
                                {
                                    converType = eCoverType.Comprehensive.ToString();
                                }

                                string paymentTermsNmae = "";
                                var paymentTermVehicel = ePaymentTermData.FirstOrDefault(p => p.ID == item.PaymentTermId);


                                if (item.PaymentTermId == 1)
                                    paymentTermsNmae = "Annual";
                                else if (item.PaymentTermId == 4)
                                    paymentTermsNmae = "Termly";
                                else
                                    paymentTermsNmae = paymentTermVehicel.Name + " Months";

                                var vehicledetail = InsuranceContext.VehicleDetails.Single(SummaryVehicleDetails[0].VehicleDetailsId);
                                CurrencyName = servicedetail.GetCurrencyName(currencylist, vehicledetail.CurrencyId);
                                string policyPeriod = item.CoverStartDate.Value.ToString("dd/MM/yyyy") + " - " + item.CoverEndDate.Value.ToString("dd/MM/yyyy");

                                Summeryofcover += "<tr> <td style='padding: 7px 10px; font - size:15px;'>" + item.RegistrationNo + " </td> <td style='padding: 7px 10px; font - size:15px;'>" + vehicledescription + "</td><td style='padding: 7px 10px; font - size:15px;'>" + CurrencyName + item.SumInsured + "</td><td style='padding: 7px 10px; font - size:15px;'>" + converType + "</td><td style='padding: 7px 10px; font - size:15px;'>" + InsuranceContext.VehicleUsages.All(Convert.ToString(item.VehicleUsage)).Select(x => x.VehUsage).FirstOrDefault() + "</td> <td style='padding: 7px 10px; font - size:15px;'>" + policyPeriod + "</td><td style='padding: 7px 10px; font - size:15px;'>" + paymentTermsNmae + "</td><td style='padding: 7px 10px; font - size:15px;'>" + CurrencyName + Convert.ToString(item.Premium) + "</td></tr>";



                            }


                            var summaryDetail = InsuranceContext.SummaryDetails.Single(model.Id);

                            if (summaryDetail != null)
                            {
                                model.CustomSumarryDetilId = summaryDetail.Id;
                            }

                            string filepath = System.Configuration.ConfigurationManager.AppSettings["urlPath"];
                            var customerQuotation = InsuranceContext.Customers.Single(summaryDetail.CustomerId);
                            var user = UserManager.FindById(customerQuotation.UserID);
                            //var SummaryVehicleDetails = InsuranceContext.SummaryVehicleDetails.All(where: $"SummaryDetailId={model.Id}").ToList();
                            var vehicleQuotation = InsuranceContext.VehicleDetails.Single(SummaryVehicleDetails[0].VehicleDetailsId);
                            var policyQuotation = InsuranceContext.PolicyDetails.Single(vehicleQuotation.PolicyId);
                            //  var ePaymentTermData = from ePaymentTerm e in Enum.GetValues(typeof(ePaymentTerm)) select new { ID = (int)e, Name = e.ToString() };
                            var paymentTerm = ePaymentTermData.FirstOrDefault(p => p.ID == vehicleQuotation.PaymentTermId);


                            Insurance.Service.EmailService objEmailService = new Insurance.Service.EmailService();

                            string QuotationEmailPath = "/Views/Shared/EmaiTemplates/QuotationEmail.cshtml";

                            string urlPath = WebConfigurationManager.AppSettings["urlPath"];

                            string rootPath = urlPath + "/CustomerRegistration/SummaryDetail?summaryDetailId=" + summaryDetail.Id;

                            // need to do work

                            // Product name

                            int agentId= summaryDetail.AgentId;
                            var agentDetials = InsuranceContext.Customers.Single(where: "Id=" + agentId);
                            var agentDetialsByUserId = UserManager.FindById(agentDetials.UserID);
                            var agentLogoDeatils = InsuranceContext.AgentLogos.Single(where: "CustomerId=" + agentId);


                            string MotorBody = System.IO.File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath(QuotationEmailPath));
                            var Bodyy = MotorBody.Replace("##PolicyNo##", policyQuotation.PolicyNumber).Replace("##path##", filepath+ agentLogoDeatils.LogoPath).Replace("##Cellnumber##", user.PhoneNumber)
                                  .Replace("#AgentFirstName#", agentDetials.FirstName).Replace("#AgentLastName#", agentDetials.LastName)
                 .Replace("#AgentAddress1#", agentDetials.AddressLine1).Replace("#AgentCity#", agentDetials.City)
                  .Replace("#AgentPhone#", agentDetials.PhoneNumber).Replace("#AgentWhatsapp#", agentDetials.AgentWhatsapp)
                  .Replace("#AgentEmail#", agentDetialsByUserId.UserName).
                                Replace("##FirstName##", customerQuotation.FirstName).Replace("##LastName##", customerQuotation.LastName).Replace("##Email##", user.Email).
                                Replace("##BirthDate##", customerQuotation.DateOfBirth.Value.ToString("dd/MM/yyyy")).Replace("##Address1##", customerQuotation.AddressLine1).
                                Replace("##Address2##", customerQuotation.AddressLine2).Replace("##Renewal##", vehicleQuotation.RenewalDate.Value.ToString("dd/MM/yyyy")).
                                Replace("##InceptionDate##", vehicleQuotation.CoverStartDate.Value.ToString("dd/MM/yyyy")).Replace("##package##", paymentTerm.Name + " Months").
                                Replace("##Summeryofcover##", Summeryofcover).Replace("##PaymentTerm##", (vehicleQuotation.PaymentTermId == 1 ? paymentTerm.Name + "(1 Year)" : paymentTerm.Name + " Months")).
                                Replace("##TotalPremiumDue##", Convert.ToString(summaryDetail.TotalPremium)).Replace("##StampDuty##", Convert.ToString(summaryDetail.TotalStampDuty)).
                                Replace("##MotorLevy##", Convert.ToString(summaryDetail.TotalZTSCLevies)).
                                Replace("##PremiumDue##", Convert.ToString(summaryDetail.TotalPremium - summaryDetail.TotalStampDuty - summaryDetail.TotalZTSCLevies - summaryDetail.TotalRadioLicenseCost + ListOfVehicles.Sum(x => x.Discount) - ListOfVehicles.Sum(x => x.VehicleLicenceFee))).
                                Replace("##PostalAddress##", customerQuotation.Zipcode).Replace("##ExcessBuyBackAmount##", Convert.ToString(ExcessBuyBackAmount)).
                                Replace("##MedicalExpenses##", Convert.ToString(MedicalExpensesAmount)).Replace("##PassengerAccidentCover##", Convert.ToString(PassengerAccidentCoverAmount)).
                                Replace("##RoadsideAssistance##", Convert.ToString(RoadsideAssistanceAmount)).Replace("##RadioLicence##", Convert.ToString(summaryDetail.TotalRadioLicenseCost)).
                                Replace("##Discount##", Convert.ToString(ListOfVehicles.Sum(x => x.Discount)))
                                .Replace("##ExcessAmount##", Convert.ToString(ExcessAmount))
                                .Replace("##CurrencyNames##", CurrencyName).
                                Replace("##SummaryDetailsPath##", Convert.ToString(rootPath)).Replace("##insurance_period##", vehicleQuotation.CoverStartDate.Value.ToString("dd/MM/yyyy") + " - " + vehicleQuotation.CoverEndDate.Value.ToString("dd/MM/yyyy")).
                                Replace("##NINumber##", customerQuotation.NationalIdentificationNumber).Replace("##VehicleLicenceFee##", Convert.ToString(ListOfVehicles.Sum(x => x.VehicleLicenceFee)));

                            #region Invoice PDF
                            var attacehmetn_File = MiscellaneousService.EmailPdf(Bodyy, policyQuotation.CustomerId, policyQuotation.PolicyNumber, "Quotation");
                            #endregion

                            #region Invoice EMail
                            //var _yAtter = "~/Pdf/14809 Gene Insure Motor Policy Book.pdf";
                            List<string> _attachementss = new List<string>();
                            _attachementss.Add(attacehmetn_File);
                            //_attachementss.Add(_yAtter);


                            if (customer.IsCustomEmail)
                            {
                                objEmailService.SendEmail(LoggedUserEmail(), "", "", "Quotation", Bodyy, _attachementss);
                            }
                            else
                            {
                                objEmailService.SendEmail(user.Email, "", "", "Quotation", Bodyy, _attachementss);
                            }


                            #endregion

                            #region Send Quotation SMS
                            Insurance.Service.smsService objsmsService = new Insurance.Service.smsService();

                            // done
                            string Recieptbody = "Hi " + customer.FirstName + "\nPlease pay" + "$" + Convert.ToString(summaryDetail.AmountPaid) + " to merchant code 249341 activate your policy with GeneInsure. Shortcode *151*2*2*249341*<amount>#." + "\n" + "\nThank you.";
                            var Recieptresult = await objsmsService.SendSMS(customer.CountryCode.Replace("+", "") + user.PhoneNumber, Recieptbody);

                            SmsLog objRecieptsmslog = new SmsLog()
                            {
                                Sendto = user.PhoneNumber,
                                Body = Recieptbody,
                                Response = Recieptresult,
                                CreatedBy = customer.Id,
                                CreatedOn = DateTime.Now
                            };

                            InsuranceContext.SmsLogs.Insert(objRecieptsmslog);
                            #endregion


                            Session.Remove("CustomerDataModal");
                            Session.Remove("PolicyData");
                            Session.Remove("VehicleDetails");
                            Session.Remove("SummaryDetailed");
                            Session.Remove("CardDetail");
                            Session.Remove("issummaryformvisited");
                            Session.Remove("PaymentId");
                            Session.Remove("InvoiceId");


                            TempData["SucessMsg"] = "Quotation has been sent email sucessfully.";


                            bool _userLoggedin = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;

                            if (_userLoggedin)
                            {
                                return RedirectToAction("QuotationList", "Account");
                            }
                            else
                            {
                                return Redirect("/CustomerRegistration/index");
                            }

                            // return RedirectToAction("SummaryDetail");
                        }
                        #endregion

                        // return RedirectToAction("InitiatePaynowTransaction", "Paypal", new { id = DbEntry.Id, TotalPremiumPaid = Convert.ToString(model.AmountPaid), PolicyNumber = policy.PolicyNumber, Email = customer.EmailAddress });

                        if (model.PaymentMethodId == 1)
                            return RedirectToAction("SaveDetailList", new { id = DbEntry.Id, invoiceNumer = model.InvoiceNumber, Paymentid = model.PaymentMethodId.Value, agentId=DbEntry.AgentId });
                        if (model.PaymentMethodId == (int)paymentMethod.ecocash)
                        {

                            //return RedirectToAction("InitiatePaynowTransaction", "Paypal", new { id = DbEntry.Id, TotalPremiumPaid = Convert.ToString(model.AmountPaid), PolicyNumber = policy.PolicyNumber, Email = customer.EmailAddress });
                            TempData["PaymentMethodId"] = model.PaymentMethodId;
                            //  return RedirectToAction("makepayment", new { id = DbEntry.Id, TotalPremiumPaid = Convert.ToString(model.AmountPaid), model.PaymentMethodId }); for paynow

                            return RedirectToAction("SaveDetailList", "Paypal", new { id = DbEntry.Id, invoiceNumer = model.InvoiceNumber, Paymentid= model.PaymentMethodId.Value });

                        }
                        else if (model.PaymentMethodId == (int)paymentMethod.Zimswitch)
                        {
                            TempData["PaymentMethodId"] = model.PaymentMethodId;
                            return RedirectToAction("IceCashPayment", "Paypal", new { id = model.Id, amount = Convert.ToString(model.AmountPaid), Paymentid = model.PaymentMethodId.Value });
                        }
                       

                        else
                            return RedirectToAction("PaymentDetail", new { id = DbEntry.Id, invoiceNumer = model.InvoiceNumber, Paymentid = model.PaymentMethodId.Value });
                    }
                    else
                    {
                        return RedirectToAction("SummaryDetail");
                    }
                }
                else
                {
                    return RedirectToAction("SummaryDetail");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("SummaryDetail");
            }
        }


        public int GetAgentId(int? staffId)
        {
            int agetnId = 0;

            var dbCustomer = InsuranceContext.Customers.Single(where: "Id="+staffId);
            if(dbCustomer!=null)
            {
                agetnId = dbCustomer.Id;
            }
            return agetnId;
        }


        public string LoggedUserEmail()
        {
            string email = "";
            bool _userLoggedin = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
            if (_userLoggedin)
            {
                var _User = UserManager.FindById(User.Identity.GetUserId().ToString());
                email = _User.Email;
            }
            return email;
        }


        public async Task<ActionResult> SaveDetailList(Int32 id, string invoiceNumber = "", string Paymentid = "", int agentId=0)
        {
            //var PaymentId = Session["PaymentId"];
            //var InvoiceId = Session["InvoiceId"];
            if (id == 0)
            {
                id = Convert.ToInt32(TempData["SummaryId"]);
                if (id == 0)
                {
                    id = Convert.ToInt32(TempData["PaymentDetail"]);
                }
            }

            SummaryDetailService detailService = new SummaryDetailService();


            var currencylist = detailService.GetAllCurrency();
            string currencyName = "$";



            string PaymentMethod = "";
            if (Paymentid == "1")
            {
                PaymentMethod = "CASH";
            }
            else if (Paymentid == "2")
            {
                PaymentMethod = "MasterCard";
            }
            else if (Paymentid == "3")
            {
                // PaymentMethod = "paynow";

                PaymentMethod = "EcoCash";
            }
            else if (Paymentid == "6")
            {
                // PaymentMethod = "paynow";

                PaymentMethod = "Zimswitch";
            }
            else if (Paymentid == "")
            {
                PaymentMethod = "CASH";
            }


            if (Paymentid == Convert.ToString((int)paymentMethod.ecocash))
            {
                var resultIceCash = ApproveVRNToIceCash(id, Convert.ToInt16(Paymentid));

                if (resultIceCash != "Approved")
                {
                    return RedirectToAction("SummaryDetail", "CustomerRegistration", new { summaryDetailId = id, paymentError = resultIceCash });
                }

            }




            var summaryDetail = InsuranceContext.SummaryDetails.Single(id);

            if (summaryDetail != null && summaryDetail.isQuotation)
            {
                summaryDetail.isQuotation = false;
                InsuranceContext.SummaryDetails.Update(summaryDetail);
            }

            var SummaryVehicleDetails = InsuranceContext.SummaryVehicleDetails.All(where: $"SummaryDetailId={id}").ToList();
            var vehicle = InsuranceContext.VehicleDetails.Single(SummaryVehicleDetails[0].VehicleDetailsId);

            var policy = InsuranceContext.PolicyDetails.Single(vehicle.PolicyId);
            //Generate QR Code
            var path = SaveQRCode(policy.PolicyNumber);

            var customer = InsuranceContext.Customers.Single(summaryDetail.CustomerId);
            var product = InsuranceContext.Products.Single(Convert.ToInt32(vehicle.ProductId));
            // var currency = InsuranceContext.Currencies.Single(policy.CurrencyId);
            var paymentInformations = InsuranceContext.PaymentInformations.SingleCustome(id);
            var user = UserManager.FindById(customer.UserID);

            var DebitNote = summaryDetail.DebitNote;
            PaymentInformation objSaveDetailListModel = new PaymentInformation();
            objSaveDetailListModel.CurrencyId = policy.CurrencyId;
            objSaveDetailListModel.PolicyId = vehicle.PolicyId;
            objSaveDetailListModel.CustomerId = summaryDetail.CustomerId.Value;
            objSaveDetailListModel.SummaryDetailId = id;
            objSaveDetailListModel.DebitNote = summaryDetail.DebitNote;
            objSaveDetailListModel.ProductId = product.Id;
            //objSaveDetailListModel.PaymentId = PaymentId == null ? "CASH" : PaymentId.ToString();
            //objSaveDetailListModel.InvoiceId = InvoiceId == null ? "" : InvoiceId.ToString();
            objSaveDetailListModel.PaymentId = PaymentMethod;
            objSaveDetailListModel.InvoiceId = invoiceNumber;

            objSaveDetailListModel.CreatedBy = customer.Id;
            objSaveDetailListModel.CreatedOn = DateTime.Now;
            //objSaveDetailListModel.InvoiceNumber = invoiceNumber;
            objSaveDetailListModel.InvoiceNumber = policy.PolicyNumber;
            List<VehicleDetail> ListOfVehicles = new List<VehicleDetail>();

            //if (paymentInformations == null)
            //{


            string filepath = System.Configuration.ConfigurationManager.AppSettings["urlPath"];
            Insurance.Service.EmailService objEmailService = new Insurance.Service.EmailService();
            string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
            bool userLoggedin = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;


            var dbPaymentInformation = InsuranceContext.PaymentInformations.Single(where: $"SummaryDetailId='{id}'");
            if (dbPaymentInformation == null)
            {
                InsuranceContext.PaymentInformations.Insert(objSaveDetailListModel);
            }
            else
            {
                objSaveDetailListModel.Id = dbPaymentInformation.Id;
                InsuranceContext.PaymentInformations.Update(objSaveDetailListModel);
            }



            if (Paymentid != Convert.ToString((int)paymentMethod.ecocash))
            {
                if (string.IsNullOrEmpty(Paymentid))
                    Paymentid = "1";

                string res = ApproveVRNToIceCash(id, Convert.ToInt16(Paymentid));
            }




            //if (!userLoggedin)
            //{

            var agentDetials  = InsuranceContext.Customers.Single(where: "Id=" + agentId);
            var agentDetialsByUserId = UserManager.FindById(agentDetials.UserID);
            var agentLogoDeatils = InsuranceContext.AgentLogos.Single(where : "CustomerId=" + agentId);


            string emailTemplatePath = "/Views/Shared/EmaiTemplates/AgentUserRegistration.cshtml";
                string EmailBody = System.IO.File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath(emailTemplatePath));
                var Body = EmailBody.Replace(" #PolicyNumber#", policy.PolicyNumber).Replace("##path##", filepath+ agentLogoDeatils.LogoPath)
                .Replace("#TodayDate#", DateTime.Now.ToShortDateString()).Replace("#FirstName#", customer.FirstName)
                .Replace("#LastName#", customer.LastName).Replace("#Address1#", customer.AddressLine1)
                .Replace("#AgentFirstName#", agentDetials.FirstName).Replace("#AgentLastName#", agentDetials.LastName)
                 .Replace("#AgentAddress1#", agentDetials.AddressLine1).Replace("#AgentCity#", agentDetials.City)
                  .Replace("#AgentPhone#", agentDetials.PhoneNumber).Replace("#AgentWhatsapp#", agentDetials.AgentWhatsapp)
                  .Replace("#AgentEmail#", agentDetialsByUserId.UserName).
                Replace("#Address2#", customer.AddressLine2).Replace("#Email#", user.Email).Replace("#change#", callbackUrl);
                //var _yAtter = "~/Pdf/14809 Gene Insure Motor Policy Book.pdf";
                var attachementFile1 = MiscellaneousService.EmailPdf(Body, policy.CustomerId, policy.PolicyNumber, "Agent WelCome Letter ");
                List<string> _attachements = new List<string>();
                _attachements.Add(attachementFile1);
                //_attachements.Add(_yAtter);


                if (customer.IsCustomEmail) // if customer has custom email
                {
                    objEmailService.SendEmail(LoggedUserEmail(), "", "", "Account Creation", Body, _attachements);
                }
                else
                {
                    objEmailService.SendEmail(user.Email, "", "", "Account Creation", Body, _attachements);
                }

                string body = "Hello " + customer.FirstName + "\nWelcome to " + agentDetials.FirstName + " " + agentDetials.LastName +"." + " Policy number is : " + policy.PolicyNumber + "\nUsername is : " + user.Email + "\nYour Password : Geneinsure@123" + "\nPlease reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>" + "\nThank you.";
                var result = await objsmsService.SendSMS(customer.Countrycode.Replace("+", "") + user.PhoneNumber, body);

                SmsLog objsmslog = new SmsLog()
                {
                    Sendto = user.PhoneNumber,
                    Body = body,
                    Response = result,
                    CreatedBy = customer.Id,
                    CreatedOn = DateTime.Now
                };

                InsuranceContext.SmsLogs.Insert(objsmslog);
            //}

            //var data = (List<Item>)Session["itemData"];
            //if (data != null)
            //{
            //var totalprem = data.Sum(x => Convert.ToDecimal(x.price));

            // string userRegisterationEmailPath = "/Views/Shared/EmaiTemplates/UserPaymentEmail.cshtml"; 24_jan_2019

            //var currencyDetails = currencylist.FirstOrDefault(c => c.Id == vehicle.CurrencyId);
            //if(currencyDetails!=null)


            currencyName = detailService.GetCurrencyName(currencylist, vehicle.CurrencyId);



            string AgentRecieptEmailPath = "/Views/Shared/EmaiTemplates/AgentReciept.cshtml";
            string EmailBody2 = System.IO.File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath(AgentRecieptEmailPath));
            var Body2 = EmailBody2.Replace("#DATE#", DateTime.Now.ToShortDateString())
                 .Replace("#AgentFirstName#", agentDetials.FirstName).Replace("#AgentLastName#", agentDetials.LastName)
                 .Replace("#AgentAddress1#", agentDetials.AddressLine1).Replace("#AgentCity#", agentDetials.City)
                  .Replace("#AgentPhone#", agentDetials.PhoneNumber).Replace("#AgentWhatsapp#", agentDetials.AgentWhatsapp)
                  .Replace("#AgentEmail#", agentDetialsByUserId.UserName)
                .Replace("##path##", filepath+ agentLogoDeatils.LogoPath).Replace("#FirstName#", customer.FirstName)
                .Replace("#LastName#", customer.LastName)
                .Replace("#AccountName#", customer.FirstName + ", " + customer.LastName)
                .Replace("#Address1#", customer.AddressLine1).Replace("#Address2#", customer.AddressLine2)
                .Replace("#currencyName#", currencyName)
                .Replace("#QRpath#", path)
                .Replace("#Amount#", Convert.ToString(summaryDetail.AmountPaid)).Replace("#PaymentDetails#", "New Premium").Replace("#ReceiptNumber#", policy.PolicyNumber).Replace("#PaymentType#", (summaryDetail.PaymentMethodId == 1 ? "Cash" : (summaryDetail.PaymentMethodId == 2 ? "PayPal" : "PayNow")));

            #region Payment Email
            var attachementFile = MiscellaneousService.EmailPdf(Body2, policy.CustomerId, policy.PolicyNumber, "AgentInvoice");
            //var yAtter = "~/Pdf/14809 Gene Insure Motor Policy Book.pdf";
            #region Payment Email
            //objEmailService.SendEmail(User.Identity.Name, "", "", "Payment", Body2, attachementFile);
            #endregion

            List<string> attachements = new List<string>();
            attachements.Add(attachementFile);


            if (customer.IsCustomEmail) // if customer has custom email
            {
                objEmailService.SendEmail(LoggedUserEmail(), "", "", " Invoice", Body2, attachements);
            }
            else
            {
                objEmailService.SendEmail(user.Email, "", "", "  Invoice", Body2, attachements);
            }



            #endregion

            #region Send Payment SMS

            // done
            string Recieptbody = "Hello " + customer.FirstName + "\nWelcome to "+agentDetials.FirstName + " " +agentDetials.LastName +". Your payment of" + "$" + Convert.ToString(summaryDetail.AmountPaid) + " has been received. Policy number is : " + policy.PolicyNumber + "\n" + "\nThanks.";
            var Recieptresult = await objsmsService.SendSMS(customer.Countrycode.Replace("+", "") + user.PhoneNumber, Recieptbody);

            SmsLog objRecieptsmslog = new SmsLog()
            {
                Sendto = user.PhoneNumber,
                Body = Recieptbody,
                Response = Recieptresult,
                CreatedBy = customer.Id,
                CreatedOn = DateTime.Now
            };

            InsuranceContext.SmsLogs.Insert(objRecieptsmslog);

            #endregion

            foreach (var itemSummaryVehicleDetails in SummaryVehicleDetails)
            {
                var itemVehicle = InsuranceContext.VehicleDetails.Single(itemSummaryVehicleDetails.VehicleDetailsId);
                //if (itemVehicle.CoverTypeId == Convert.ToInt32(eCoverType.ThirdParty))
                //{


                MiscellaneousService.AddAgentLoyaltyPoints(summaryDetail.CustomerId.Value, policy.Id, Mapper.Map<VehicleDetail, RiskDetailModel>(itemVehicle), user.Email, filepath, agentDetials, agentLogoDeatils, agentDetialsByUserId.Email);
                //}
                ListOfVehicles.Add(itemVehicle);
            }


            #region Payment PDF
            //MiscellaneousService.EmailPdf(Body2, policy.CustomerId, policy.PolicyNumber, "Reciept Payment");
            #endregion
            //}

            decimal totalpaymentdue = 0.00m;

            //if (vehicle.PaymentTermId == 1)
            //{
            //    totalpaymentdue = (decimal)summaryDetail.TotalPremium;
            //}
            //else if (vehicle.PaymentTermId == 4)
            //{
            //    totalpaymentdue = (decimal)summaryDetail.TotalPremium * 3;
            //}
            //else if (vehicle.PaymentTermId == 3)
            //{
            //    totalpaymentdue = (decimal)summaryDetail.TotalPremium * 4;
            //}

            string Summeryofcover = "";
            var RoadsideAssistanceAmount = 0.00m;
            var MedicalExpensesAmount = 0.00m;
            var ExcessBuyBackAmount = 0.00m;
            var PassengerAccidentCoverAmount = 0.00m;
            var ExcessAmount = 0.00m;
            var ePaymentTermData = from ePaymentTerm e in Enum.GetValues(typeof(ePaymentTerm)) select new { ID = (int)e, Name = e.ToString() };




            foreach (var item in ListOfVehicles)
            {
                Insurance.Service.VehicleService obj = new Insurance.Service.VehicleService();
                VehicleModel model = InsuranceContext.VehicleModels.Single(where: $"ModelCode='{item.ModelId}'");
                VehicleMake make = InsuranceContext.VehicleMakes.Single(where: $" MakeCode='{item.MakeId}'");

                string vehicledescription = model.ModelDescription + " / " + make.MakeDescription;

                RoadsideAssistanceAmount = RoadsideAssistanceAmount + Convert.ToDecimal(item.RoadsideAssistanceAmount);
                MedicalExpensesAmount = MedicalExpensesAmount + Convert.ToDecimal(item.MedicalExpensesAmount);
                ExcessBuyBackAmount = ExcessBuyBackAmount + Convert.ToDecimal(item.ExcessBuyBackAmount);
                PassengerAccidentCoverAmount = PassengerAccidentCoverAmount + Convert.ToDecimal(item.PassengerAccidentCoverAmount);
                ExcessAmount = ExcessAmount + Convert.ToDecimal(item.ExcessAmount);

                var paymentTermVehicel = ePaymentTermData.FirstOrDefault(p => p.ID == item.PaymentTermId);

                //string   paymentTermsName = (item.PaymentTermId == 1 ? paymentTermVehicel.Name + "(1 Year)" : paymentTermVehicel.Name + " Months");

                string paymentTermsName = "";
                if (item.PaymentTermId == 1)
                    paymentTermsName = "Annual";
                else if (item.PaymentTermId == 4)
                    paymentTermsName = "Termly";
                else
                    paymentTermsName = paymentTermVehicel.Name + " Months";


                string policyPeriod = item.CoverStartDate.Value.ToString("dd/MM/yyyy") + " - " + item.CoverEndDate.Value.ToString("dd/MM/yyyy");

                //   currencyDetails = currencylist.FirstOrDefault(c => c.Id == item.CurrencyId);


                currencyName = detailService.GetCurrencyName(currencylist, item.CurrencyId);
                //Summeryofcover += "<tr><td style='padding: 7px 10px; font - size:15px;'>" + vehicledescription + "</td><td style='padding: 7px 10px; font - size:15px;'>$" + item.SumInsured + "</td><td style='padding: 7px 10px; font - size:15px;'>" + (item.CoverTypeId == 1 ? eCoverType.Comprehensive.ToString() : eCoverType.ThirdParty.ToString()) + "</td><td style='padding: 7px 10px; font - size:15px;'>" + InsuranceContext.VehicleUsages.All(Convert.ToString(item.VehicleUsage)).Select(x => x.VehUsage).FirstOrDefault() + "</td><td style='padding: 7px 10px; font - size:15px;'>$0.00</td><td style='padding: 7px 10px; font - size:15px;'>$" + Convert.ToString(item.Excess) + "</td><td style='padding: 7px 10px; font - size:15px;'>$" + Convert.ToString(item.Premium) + "</td></tr>";
                Summeryofcover += "<tr><td style='padding: 7px 10px; font - size:15px;'>" + item.RegistrationNo + " </td> <td style='padding: 7px 10px; font - size:15px;'><font size='2'>" + vehicledescription + "</font></td> <td> " + item.CoverNote + " </td><td style='padding: 7px 10px; font - size:15px;'><font size='2'>" + currencyName + item.SumInsured + "</font></td><td style='padding: 7px 10px; font - size:15px;'><font size='2'>" + (item.CoverTypeId == 4 ? eCoverType.Comprehensive.ToString() : eCoverType.ThirdParty.ToString()) + "</font></td><td style='padding: 7px 10px; font - size:15px;'><font size='2'>" + InsuranceContext.VehicleUsages.All(Convert.ToString(item.VehicleUsage)).Select(x => x.VehUsage).FirstOrDefault() + "</font></td><td style='padding: 7px 10px; font - size:15px;'><font size='2'>" + policyPeriod + "</font></td><td style='padding: 7px 10px; font - size:15px;'><font size='2'>" + paymentTermsName + "</font></td><td style='padding: 7px 10px; font - size:15px;'><font size='2'>" + currencyName + Convert.ToString(item.Premium + item.Discount) + "</font></td></tr>";


            }
            //for (int i = 0; i < SummaryVehicleDetails.Count; i++)
            //{
            //    var _vehicle = InsuranceContext.VehicleDetails.Single(SummaryVehicleDetails[i].VehicleDetailsId);

            //}


            var paymentTerm = ePaymentTermData.FirstOrDefault(p => p.ID == vehicle.PaymentTermId);
            string AgentScheduleMotorPath = "/Views/Shared/EmaiTemplates/AgentScheduleMotor.cshtml";
            string MotorBody = System.IO.File.ReadAllText(System.Web.Hosting.HostingEnvironment.MapPath(AgentScheduleMotorPath));
            //var Bodyy = MotorBody.Replace("##PolicyNo##", policy.PolicyNumber).Replace("##Cellnumber##", user.PhoneNumber).Replace("##FirstName##", customer.FirstName).Replace("##LastName##", customer.LastName).Replace("##Email##", user.Email).Replace("##BirthDate##", customer.DateOfBirth.Value.ToString("dd/MM/yyyy")).Replace("##Address1##", customer.AddressLine1).Replace("##Address2##", customer.AddressLine2).Replace("##Renewal##", vehicle.RenewalDate.Value.ToString("dd/MM/yyyy")).Replace("##InceptionDate##", vehicle.CoverStartDate.Value.ToString("dd/MM/yyyy")).Replace("##package##", paymentTerm.Name).Replace("##Summeryofcover##", Summeryofcover).Replace("##PaymentTerm##", (summaryDetail.PaymentTermId == 1 ? paymentTerm.Name + "(1 Year)" : paymentTerm.Name + "(" + summaryDetail.PaymentTermId.ToString() + "Months)")).Replace("##TotalPremiumDue##", Convert.ToString(summaryDetail.TotalPremium)).Replace("##StampDuty##", Convert.ToString(summaryDetail.TotalStampDuty)).Replace("##MotorLevy##", Convert.ToString(summaryDetail.TotalZTSCLevies)).Replace("##PremiumDue##", Convert.ToString(summaryDetail.TotalPremium - summaryDetail.TotalStampDuty - summaryDetail.TotalZTSCLevies - summaryDetail.TotalRadioLicenseCost - ListOfVehicles.Sum(x => x.Discount))).Replace("##PostalAddress##", customer.Zipcode).Replace("##ExcessBuyBackAmount##", Convert.ToString(vehicle.ExcessBuyBackAmount)).Replace("##MedicalExpenses##", Convert.ToString(vehicle.MedicalExpensesAmount)).Replace("##PassengerAccidentCover##", Convert.ToString(vehicle.PassengerAccidentCoverAmount)).Replace("##RoadsideAssistance##", Convert.ToString(vehicle.RoadsideAssistanceAmount)).Replace("##RadioLicence##", Convert.ToString(summaryDetail.TotalRadioLicenseCost)).Replace("##Discount##", Convert.ToString(vehicle.Discount));
            //  var Bodyy = MotorBody.Replace("##PolicyNo##", policy.PolicyNumber).Replace("##Cellnumber##", user.PhoneNumber).Replace("##FirstName##", customer.FirstName).Replace("##LastName##", customer.LastName).Replace("##Email##", user.Email).Replace("##BirthDate##", customer.DateOfBirth.Value.ToString("dd/MM/yyyy")).Replace("##Address1##", customer.AddressLine1).Replace("##Address2##", customer.AddressLine2).Replace("##Renewal##", vehicle.RenewalDate.Value.ToString("dd/MM/yyyy")).Replace("##InceptionDate##", vehicle.CoverStartDate.Value.ToString("dd/MM/yyyy")).Replace("##package##", paymentTerm.Name).Replace("##Summeryofcover##", Summeryofcover).Replace("##PaymentTerm##", (vehicle.PaymentTermId == 1 ? paymentTerm.Name + "(1 Year)" : paymentTerm.Name + "(" + vehicle.PaymentTermId.ToString() + "Months)")).Replace("##TotalPremiumDue##", Convert.ToString(summaryDetail.TotalPremium)).Replace("##StampDuty##", Convert.ToString(summaryDetail.TotalStampDuty)).Replace("##MotorLevy##", Convert.ToString(summaryDetail.TotalZTSCLevies)).Replace("##PremiumDue##", Convert.ToString(summaryDetail.TotalPremium - summaryDetail.TotalStampDuty - summaryDetail.TotalZTSCLevies - summaryDetail.TotalRadioLicenseCost + ListOfVehicles.Sum(x => x.Discount))).Replace("##PostalAddress##", customer.Zipcode).Replace("##ExcessBuyBackAmount##", Convert.ToString(ExcessBuyBackAmount)).Replace("##MedicalExpenses##", Convert.ToString(MedicalExpensesAmount)).Replace("##PassengerAccidentCover##", Convert.ToString(PassengerAccidentCoverAmount)).Replace("##RoadsideAssistance##", Convert.ToString(RoadsideAssistanceAmount)).Replace("##RadioLicence##", Convert.ToString(summaryDetail.TotalRadioLicenseCost)).Replace("##Discount##", Convert.ToString(vehicle.Discount)).Replace("##ExcessAmount##", Convert.ToString(ExcessAmount)).Replace("##NINumber##", customer.NationalIdentificationNumber).Replace("##VehicleLicenceFee##",Convert.ToString(vehicle.VehicleLicenceFee));

            var Bodyy = MotorBody.Replace("##PolicyNo##", policy.PolicyNumber)
                .Replace("#AgentFirstName#", agentDetials.FirstName).Replace("#AgentLastName#", agentDetials.LastName)
                 .Replace("#AgentAddress1#", agentDetials.AddressLine1).Replace("#AgentCity#", agentDetials.City)
                  .Replace("#AgentPhone#", agentDetials.PhoneNumber).Replace("#AgentWhatsapp#", agentDetials.AgentWhatsapp)
                  .Replace("#AgentEmail#", agentDetialsByUserId.UserName).
                Replace("##paht##", filepath+agentLogoDeatils.LogoPath).Replace("##Cellnumber##", user.PhoneNumber)
                .Replace("##currencyName##", currencyName)
                .Replace("##QRpath##", path)
                .Replace("##FirstName##", customer.FirstName).Replace("##LastName##", customer.LastName).Replace("##Email##", user.Email)
                .Replace("##BirthDate##", customer.DateOfBirth.Value.ToString("dd/MM/yyyy"))
                .Replace("##Address1##", customer.AddressLine1).Replace("##Address2##", customer.AddressLine2).Replace("##Renewal##", vehicle.RenewalDate.Value.ToString("dd/MM/yyyy"))
                .Replace("##InceptionDate##", vehicle.CoverStartDate.Value.ToString("dd/MM/yyyy")).Replace("##package##", paymentTerm.Name).Replace("##Summeryofcover##", Summeryofcover)
                .Replace("##PaymentTerm##", (vehicle.PaymentTermId == 1 ? paymentTerm.Name + "(1 Year)" : paymentTerm.Name + "(" + vehicle.PaymentTermId.ToString() + "Months)"))
                .Replace("##TotalPremiumDue##", Convert.ToString(summaryDetail.TotalPremium)).Replace("##StampDuty##", Convert.ToString(summaryDetail.TotalStampDuty))
                .Replace("##MotorLevy##", Convert.ToString(summaryDetail.TotalZTSCLevies))
                .Replace("##PremiumDue##", Convert.ToString(summaryDetail.TotalPremium - summaryDetail.TotalStampDuty - summaryDetail.TotalZTSCLevies - summaryDetail.TotalRadioLicenseCost - ListOfVehicles.Sum(x => x.VehicleLicenceFee) + ListOfVehicles.Sum(x => x.Discount)))
                .Replace("##PostalAddress##", customer.Zipcode).Replace("##ExcessBuyBackAmount##", Convert.ToString(ExcessBuyBackAmount)).Replace("##MedicalExpenses##", Convert.ToString(MedicalExpensesAmount))
                .Replace("##PassengerAccidentCover##", Convert.ToString(PassengerAccidentCoverAmount)).Replace("##RoadsideAssistance##", Convert.ToString(RoadsideAssistanceAmount))
                .Replace("##RadioLicence##", Convert.ToString(summaryDetail.TotalRadioLicenseCost)).Replace("##Discount##", Convert.ToString(ListOfVehicles.Sum(x => x.Discount)))
                .Replace("##ExcessAmount##", Convert.ToString(ExcessAmount)).Replace("##NINumber##", customer.NationalIdentificationNumber).Replace("##VehicleLicenceFee##", Convert.ToString(ListOfVehicles.Sum(x => x.VehicleLicenceFee)));

            //var attachementFile = MiscellaneousService.EmailPdf(Body2, policy.CustomerId, policy.PolicyNumber, "Reciept Payment");

            #region Invoice PDF
            var attacehmetnFile = MiscellaneousService.EmailPdf(Bodyy, policy.CustomerId, policy.PolicyNumber, "Agent Schedule-motor");
            var Atter = "~/Pdf/14809 Gene Insure Motor Policy Book.pdf";


            #endregion
            List<string> __attachements = new List<string>();
            __attachements.Add(attacehmetnFile);
            //if (!userLoggedin)
            //{
            __attachements.Add(Atter);
            //}

            #region Invoice EMail


            if (customer.IsCustomEmail) // if customer has custom email
            {
                objEmailService.SendEmail(LoggedUserEmail(), "", "", "Schedule-motor", Bodyy, __attachements);
            }
            else
            {
                objEmailService.SendEmail(user.Email, "", "", "Schedule-motor", Bodyy, __attachements);
            }



            #endregion

            //}

            #region Remove  All Sessions
            try
            {
                Session.Remove("CustomerDataModal");
                Session.Remove("PolicyData");
                Session.Remove("VehicleDetails");
                Session.Remove("SummaryDetailed");
                Session.Remove("CardDetail");
                Session.Remove("issummaryformvisited");
                Session.Remove("PaymentId");
                Session.Remove("InvoiceId");
            }
            catch (Exception ex)
            {
                Session.Remove("InvoiceId");
                Session.Remove("PaymentId");
                Session.Remove("issummaryformvisited");
                Session.Remove("CardDetail");
                Session.Remove("SummaryDetailed");
                Session.Remove("VehicleDetails");
                Session.Remove("PolicyData");
                Session.Remove("CustomerDataModal");
            }

            #endregion

            return RedirectToAction("ThankYou");
        }


        public string SaveQRCode(string Policyno)
        {
            string path = "";
            try
            {

                var urlPath = System.Configuration.ConfigurationManager.AppSettings["urlPath"];

                Insurance.Domain.QRCode Codes = new Insurance.Domain.QRCode();

                //var Policy =Convert.ToString (TempData["Registrationno"]);

                using (MemoryStream ms = new MemoryStream())
                {

                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(Policyno, QRCodeGenerator.ECCLevel.Q);
                    QRCoder.QRCode QrCode = new QRCoder.QRCode(qrCodeData);
                    using (Bitmap bitMap = QrCode.GetGraphic(6))
                    {
                        bitMap.Save(ms, ImageFormat.Png);
                        Base64ToImage(Convert.ToBase64String(ms.ToArray())).Save(Server.MapPath("~/QRCode/" + Policyno + ".jpg"));
                        //path = "/QRCode/" + Policyno + ".jpg";
                        path = urlPath + "/QRCode/" + Policyno + ".jpg";
                    }

                    //path = Request.Url.Scheme + System.Uri.SchemeDelimiter + "/" + Request.Url.Host + "/QRCode/" + Policyno + ".jpg";


                    //LinkedResource lr = new LinkedResource("path",MediaTypeNames.Image.Jpeg);
                    //lr.ContentId = "qrImage";
                    //path = Server.MapPath("~/QRCode/" + Policyno + ".jpg");
                    //LinkedResource lr = new LinkedResource(path, MediaTypeNames.Image.Jpeg);
                    //lr.ContentId = "image1";
                    //AlternateView av = AlternateView.CreateAlternateViewFromString(str, null, MediaTypeNames.Text.Html);
                    //lr.ContentId = "image1";
                    //av.LinkedResources.Add(lr);
                    //message.AlternateViews.Add(av);


                    // path = "https://gene.co.zw/QRCode/" + Policyno + ".jpg";
                    // path = Url.ss "http://geneinsureclaim.kindlebit.com/QRCode"  + Policyno + ".jpg";

                    //path = Request.Url.Authority+"/QRCode/" + Policyno + ".jpg";

                    // path = "/QRCode/" + Policyno + ".jpg";


                    Codes.PolicyNo = Policyno;
                    Codes.Qrcode = Policyno;
                    Codes.ReadBy = "";
                    Codes.Deliverto = "";
                    Codes.Createdon = DateTime.Now;
                    Codes.Comment = "";

                    //   var QRCodedata = Mapper.Map<QRCode, QRCode>(Codes);
                    InsuranceContext.QRCodes.Insert(Codes);
                }

            }
            catch (Exception ex)
            {

            }

            return path;
        }


        public System.Drawing.Image Base64ToImage(string base64String)
        {

            byte[] imageBytes = Convert.FromBase64String(base64String.ToString());
            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
            ms.Write(imageBytes, 0, imageBytes.Length);
            System.Drawing.Image image = System.Drawing.Image.FromStream(ms, true);
            return image;
        }


        public string ApproveVRNToIceCash(int id, int paymentMethod)
        {
            #region update  TPIQuoteUpdate
            Insurance.Service.EmailService log = new Insurance.Service.EmailService();

            string result = "";

            try
            {
                var tokenObject = new ICEcashTokenResponse();
                var PartnerToken = "";

                var customerDetails = new Customer();
                ICEcashService iceCash = new ICEcashService();
                var summaryDetial = InsuranceContext.SummaryVehicleDetails.All(where: $"SummaryDetailId = '" + id + "'");

                if (summaryDetial != null)
                {

                    foreach (var item in summaryDetial)
                    {

                        var vichelDetails = InsuranceContext.VehicleDetails.Single(item.VehicleDetailsId);
                        if (vichelDetails != null)
                        {
                            string InsuranceID = vichelDetails.InsuranceId;


                            // InsuranceID is null

                            if (InsuranceID == null)
                            {
                                iceCash.getToken();

                                if (Session["ICEcashToken"] != null)
                                    tokenObject = (ICEcashTokenResponse)Session["ICEcashToken"];



                                List<RiskDetailModel> objVehicles = new List<RiskDetailModel>();
                                //objVehicles.Add(new RiskDetailModel { RegistrationNo = regNo });
                                objVehicles.Add(new RiskDetailModel { RegistrationNo = vichelDetails.RegistrationNo, PaymentTermId = Convert.ToInt32(vichelDetails.PaymentTermId) });
                                tokenObject = (ICEcashTokenResponse)Session["ICEcashToken"];


                                ResultRootObject quoteresponse = iceCash.checkVehicleExists(objVehicles, tokenObject.Response.PartnerToken, tokenObject.PartnerReference);

                                // if partern token expire
                                if (quoteresponse.Response.Result != 0)
                                {
                                    if (quoteresponse.Response.Quotes[0] != null)
                                    {
                                        vichelDetails.InsuranceId = quoteresponse.Response.Quotes[0].InsuranceID;
                                    }
                                }
                            }

                            // end is null
                            customerDetails = InsuranceContext.Customers.Single(vichelDetails.CustomerId);

                            //if (customerDetails != null)
                            //{
                            //    var _user = UserManager.FindById(customerDetails.UserID);

                            //    var customerEmail = _user.Email;
                            //}

                            var policyDetils = InsuranceContext.PolicyDetails.Single(vichelDetails.PolicyId);

                            if (policyDetils != null)
                            {
                                var policyNum = policyDetils.PolicyNumber;
                            }
                        }

                        if (vichelDetails != null && vichelDetails.InsuranceId != null)
                        {

                            //if (Session["ICEcashToken"] != null)
                            //{
                            //    var icevalue = (ICEcashTokenResponse)Session["ICEcashToken"];
                            //    string format = "yyyyMMddHHmmss";
                            //    var IceDateNowtime = DateTime.Now;
                            //    var IceExpery = DateTime.ParseExact(icevalue.Response.ExpireDate, format, CultureInfo.InvariantCulture);
                            //    if (IceDateNowtime > IceExpery)
                            //    {
                            //        iceCash.getToken();
                            //    }
                            //    tokenObject = (ICEcashTokenResponse)Session["ICEcashToken"];
                            //}
                            //else
                            //{
                            iceCash.getToken();
                            tokenObject = (ICEcashTokenResponse)HttpContext.Session["ICEcashToken"];
                            //}



                            PartnerToken = tokenObject.Response.PartnerToken;

                            ResultRootObject quoteresponse = ICEcashService.TPIQuoteUpdate(customerDetails, vichelDetails, PartnerToken, paymentMethod);

                            // if partern token expire
                            //if (quoteresponse.Response.Result == 0)
                            //{
                            if (quoteresponse.Response != null && quoteresponse.Response.Message.Contains("Partner Token has expired"))
                            {
                                //  log.WriteLog(quoteresponse.Response.Quotes[0].Message + " reg no: " + vichelDetails.RegistrationNo);
                                iceCash.getToken();
                                tokenObject = (ICEcashTokenResponse)Session["ICEcashToken"];
                                PartnerToken = tokenObject.Response.PartnerToken;
                                ICEcashService.TPIQuoteUpdate(customerDetails, vichelDetails, PartnerToken, paymentMethod);
                            }
                            //}

                            //   System.Threading.Thread.Sleep(10000); // wait for 20 second

                            var res = ICEcashService.TPIPolicy(vichelDetails, PartnerToken);

                            if (res.Response != null && res.Response.Message.Contains("Partner Token has expired"))
                            {
                                iceCash.getToken();
                                tokenObject = (ICEcashTokenResponse)Session["ICEcashToken"];
                                res = ICEcashService.TPIPolicy(vichelDetails, PartnerToken);
                            }


                            if (res.Response != null && res.Response.Message == "Policy Retrieved")
                            {

                                //if (res.Response.Status == "Approved")
                                //{
                                result = res.Response.Status;
                                vichelDetails.InsuranceStatus = "Approved";
                                vichelDetails.CoverNote = res.Response.PolicyNo;
                                //  vichelDetails.CoverNote = res.o
                                InsuranceContext.VehicleDetails.Update(vichelDetails);
                                //}
                                //else
                                //{
                                //    result = res.Response.Status;
                                //}
                            }
                            else
                            {
                                result = res.Response.Message;
                            }
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                // log.WriteLog("to approve");

            }

            return result;
            #endregion
        }


        public ActionResult ThankYou()
        {
            return View();
        }




    }
}