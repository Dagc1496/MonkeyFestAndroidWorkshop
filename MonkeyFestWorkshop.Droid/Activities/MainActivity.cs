﻿using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Widget;
using Autofac;
using Firebase;
using Firebase.Database;
using MonkeyFestWorkshop.Core.DomainServices;
using MonkeyFestWorkshop.Core.Factories;
using MonkeyFestWorkshop.Domain.Enumerations;
using MonkeyFestWorkshop.Domain.Exceptions;
using MonkeyFestWorkshop.Domain.Models.Menu;
using MonkeyFestWorkshop.Domain.Models.User;
using MonkeyFestWorkshop.Domain.Models.Vehicle;
using MonkeyFestWorkshop.Droid.Adapters;
using Newtonsoft.Json;

namespace MonkeyFestWorkshop.Droid.Activities
{
    [Activity]
    public class MainActivity : BaseActivity, IValueEventListener
    {
        private RecyclerView recyclerView;
        private const string authenticatedUser = "1111";
        private UserServiceDomain userServiceDomain;
        private VehicleFactory vehicleFactory;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            FirebaseApp.InitializeApp(Application.Context);

            SetDependencies();
            GetUserInfo();
            LoadVehicles();
        }

        private void SetDependencies()
        {
            userServiceDomain = ConfigureDependencies().Resolve<UserServiceDomain>();
        }

        private void GetUserInfo()
        {
            try
            {
                UserInfo userInfo = userServiceDomain.GetUserInfo(authenticatedUser);
                Title = userInfo.Name;
            }
            catch (NetworkException ex)
            {
                Title = string.Empty;
                Toast.MakeText(this, ex.Message, ToastLength.Short).Show();
            }
            catch (System.Exception ex)
            {
                Title = string.Empty;
                Log.Error(ComponentName.ToString(), ex.Message);
                Toast.MakeText(this, "Ha ocurrido un error inesperado.", ToastLength.Short).Show();
            }
        }

        private void ConfigRecyclerView(List<SectionItem> menu)
        {
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView_vehicles);
            var adapter = new SectionMenuAdapter(menu);
            adapter.OnItemClick += Adapter_OnItemClick;
            recyclerView.SetLayoutManager(new LinearLayoutManager(this));
            recyclerView.SetAdapter(adapter);
        }

        private void LoadVehicles()
        {
            FirebaseDatabase database = FirebaseDatabase.Instance;
            DatabaseReference reference = database.GetReference("vehicle");
            reference.AddValueEventListener(this);
        }

        private void Adapter_OnItemClick(object sender, BaseVehicle vehicle)
        {
            GoToDetailsActivity(vehicle);
        }

        private void GoToDetailsActivity(BaseVehicle vehicle)
        {
            var intent = new Intent(this, typeof(VehicleDetailActivity));
            var extras = new Bundle();
            extras.PutString("vehicle", JsonConvert.SerializeObject(vehicle));

            intent.PutExtras(extras);

            StartActivity(intent);
        }

        public void OnCancelled(DatabaseError error)
        {
            Log.Debug(ComponentName.PackageName, error.Code.ToString());
        }

        public void OnDataChange(DataSnapshot snapshot)
        {
            List<BaseVehicle> list = new List<BaseVehicle>();
            for (int i = 0; i < snapshot.ChildrenCount; i++)
            {
                DataSnapshot dataSnapshot = snapshot.Child(i.ToString());

                var car = new Car
                {
                    Id = dataSnapshot.Child("id").Value.ToString(),
                    Plate = dataSnapshot.Child("plate").Value.ToString(),
                    Model = dataSnapshot.Child("model").Value.ToString(),
                    Line = dataSnapshot.Child("line").Value.ToString(),
                    BrandName = dataSnapshot.Child("brand_name").Value.ToString(),
                    Price = dataSnapshot.Child("price").Value.ToString()
                };

                if (dataSnapshot.Child("featured").Value is Java.Lang.Boolean)
                {
                    var featured = dataSnapshot.Child("featured").Value as Java.Lang.Boolean;
                    car.Featured = featured.BooleanValue();
                }
                
                list.Add(car);
            }

            vehicleFactory = new VehicleFactory(list);
            List<BaseVehicle> orderList = vehicleFactory.Create(VehicleCategory.Normal);
            List<BaseVehicle> featuredVehicles = vehicleFactory.Create(VehicleCategory.Featured);

            List<SectionItem> sectionItems = new List<SectionItem>();

            var featuredItems = new SectionItem
            {
                Title = "Destacados",
                Vehicles = featuredVehicles,
                SectionType = SectionType.Featured
            };

            var classicItems = new SectionItem
            {
                Title = "Vehículos",
                Vehicles = orderList,
                SectionType = SectionType.Classic
            };

            sectionItems.Add(featuredItems);
            sectionItems.Add(classicItems);
             
            ConfigRecyclerView(sectionItems);
        }
    }
}

