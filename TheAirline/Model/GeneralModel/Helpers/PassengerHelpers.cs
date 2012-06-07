﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheAirline.Model.AirlineModel;
using TheAirline.Model.GeneralModel.StatisticsModel;
using TheAirline.Model.AirlinerModel.RouteModel;
using TheAirline.Model.AirportModel;
using TheAirline.Model.AirlinerModel;
using TheAirline.Model.GeneralModel.Helpers;
using TheAirline.Model.PassengerModel;

namespace TheAirline.Model.GeneralModel
{
    //the helper class for the passengers
    public class PassengerHelpers
    {
        private static Dictionary<Airline, double> HappinessPercent = new Dictionary<Airline, double>();
        private static Random rnd = new Random();
        //returns the passengers happiness for an airline
        public static double GetPassengersHappiness(Airline airline)
        {
            double passengers = 0;
            foreach (int year in airline.Statistics.getYears())
                passengers += Convert.ToDouble(airline.Statistics.getStatisticsValue(year, StatisticsTypes.GetStatisticsType("Passengers")));
            double value = GetHappinessValue(airline);

            if (passengers == 0)
                return 0;
            else
                return value / passengers * 100.0;

        }
        //adds happiness to an airline
        public static void AddPassengerHappiness(Airline airline)
        {
            if (HappinessPercent.ContainsKey(airline))
                HappinessPercent[airline] += 1;
            else
                HappinessPercent.Add(airline, 1);
        }
        // chs, 2011-13-10 added for loading of passenger happiness
        public static void SetPassengerHappiness(Airline airline, double value)
        {
            if (HappinessPercent.ContainsKey(airline))
                HappinessPercent[airline] = value;
            else
                HappinessPercent.Add(airline, value);
        }
        public static double GetHappinessValue(Airline airline)
        {
            if (HappinessPercent.ContainsKey(airline))
                return HappinessPercent[airline];
            else
                return 0;
        }
        public static List<Passenger> GetFlightPassengers(FleetAirliner airliner, AirlinerClass.ClassType type)
        {
            int seatingCapacity = airliner.Airliner.getAirlinerClass(type).SeatingCapacity;

            Airport airportCurrent = Airports.GetAirport(airliner.CurrentPosition);
            Airport destination = airliner.CurrentFlight.Entry.Destination.Airport;

            List<Passenger> passengers = new List<Passenger>();

            if (airportCurrent.getPassengers(destination).FindAll(p => p.PreferedClass == type).Sum(p => p.Factor) > seatingCapacity)
            {
                List<Passenger> tPassengers = airportCurrent.getPassengers(destination);

                int i = 0;

                while (passengers.Sum(p => p.Factor) + tPassengers[i].Factor <= seatingCapacity)
                {
                    passengers.Add(tPassengers[i]);
                    i++;
                }
                int tFactor = tPassengers[i].Factor;

                tPassengers[i].Factor = seatingCapacity - passengers.Sum(p => p.Factor);
                passengers.Add(tPassengers[i]);

                Passenger newPassenger = new Passenger(Guid.NewGuid().ToString(), tPassengers[i].PrimaryType, tPassengers[i].HomeAirport, tPassengers[i].PreferedClass);
                newPassenger.Updated = GameObject.GetInstance().GameTime;
                newPassenger.Destination = tPassengers[i].Destination;
                newPassenger.Factor = tFactor - tPassengers[i].Factor;
                newPassenger.Route = tPassengers[i].Route;
                newPassenger.CurrentAirport = tPassengers[i].CurrentAirport;

                Passengers.AddPassenger(newPassenger);
                airportCurrent.addPassenger(newPassenger);


            }
            else
                passengers = airportCurrent.getPassengers(destination);

            passengers.ForEach(p => airportCurrent.removePassenger(p));
            passengers.ForEach(p => p.CurrentAirport = null);

            return passengers;
        }
        //returns a random destination from an airport
        private static Airport GetRandomDestination(Airport currentAirport)
        {
            Dictionary<Airport, int> airportsList = new Dictionary<Airport, int>();
            Airports.GetAirports().FindAll(a => a != currentAirport).ForEach(a => airportsList.Add(a, (int)a.Profile.Size * (a.Profile.Country == currentAirport.Profile.Country ? 7 : 3)));

            return AIHelpers.GetRandomItem(airportsList);
        }
        //updates a landed passenger
        public static void UpdateLandedPassenger(Passenger passenger, Airport currentAirport)
        {
            
            if (passenger.Destination == currentAirport)
            {
                if (passenger.HomeAirport == currentAirport)
                {

                    passenger.Destination = GetRandomDestination(currentAirport) ;
                }
                else
                    passenger.Destination = passenger.HomeAirport;
                passenger.Route = FindPassengerRoute(currentAirport, passenger);
            }
            passenger.Updated = GameObject.GetInstance().GameTime;
            currentAirport.addPassenger(passenger);
            passenger.CurrentAirport = currentAirport;
        }
        /*
        //returns the number of passengers for a flight
        public static int GetFlightPassengers(FleetAirliner airliner, AirlinerClass.ClassType type)
        {
            Airport airportCurrent = Airports.GetAirport(airliner.CurrentPosition);
            Airport airportDestination = airliner.CurrentFlight.Entry.Destination.Airport;

            int totalRoutes1 = airportCurrent.Terminals.getRoutes().Count;
            int totalRoutes2 = airportDestination.Terminals.getRoutes().Count;

            int sameRoutes = 0;
      
            foreach (Route route in airportCurrent.Terminals.getRoutes())
                if (route.Destination1 == airportDestination || route.Destination2 == airportDestination)
                    sameRoutes++;

            int destSize = (int)airportDestination.Profile.Size;
            int deptSize = (int)airportCurrent.Profile.Size;

            double size = (1000 * destSize * GetSeasonFactor(airportDestination)) + (750 * deptSize * GetSeasonFactor(airportCurrent));
            size = size / (sameRoutes + 1);
            size = size / totalRoutes1; 
            size = size / totalRoutes2;
           
            if (double.IsInfinity(size))
            {
                double seasonFactor = (750 * deptSize * GetSeasonFactor(airportCurrent));
                double seasonFactor2 = (1000 * destSize * GetSeasonFactor(airportDestination));

                sameRoutes = Convert.ToInt32(seasonFactor + seasonFactor2);
        }
           

            double happiness = GetPassengersHappiness(airliner.Airliner.Airline) > 0 ? GetPassengersHappiness(airliner.Airliner.Airline) : 35.0;

            size = Convert.ToDouble(size) * happiness / 100.0;

            double minValue = Math.Min(size, airliner.Airliner.getAirlinerClass(type).SeatingCapacity)*0.8;

            int value = rnd.Next((int)minValue, Math.Min(Math.Max(10,(int)size), airliner.Airliner.getAirlinerClass(type).SeatingCapacity));

            if (airportCurrent.IsHub)
            {
                double hubCoeff = 1.2;
                double dValue = Convert.ToDouble(value) * hubCoeff;
                value = Math.Min((int)dValue, airliner.Airliner.getAirlinerClass(type).SeatingCapacity);
            }

            double price = airliner.CurrentFlight.Entry.TimeTable.Route.getRouteAirlinerClass(type).FarePrice;
            double standardPrice = GetPassengerPrice(airliner.CurrentFlight.Entry.TimeTable.Route.Destination1, airliner.CurrentFlight.Entry.TimeTable.Route.Destination2);

            double priceDiff = (price / standardPrice) * 1.13;
     
            value = Math.Min((int)(Convert.ToDouble(value) / priceDiff),airliner.Airliner.getAirlinerClass(type).SeatingCapacity);

            if (value < 15)
                value = rnd.Next(value, 15);

            return value;

        


        }
         * */
        //returns the season factor for an airport
        private static double GetSeasonFactor(Airport airport)
        {
            Boolean isSummer = GameObject.GetInstance().GameTime.Month >= 3 && GameObject.GetInstance().GameTime.Month < 9;

            if (airport.Profile.Season == Weather.Season.All_Year)
                return 1;
            if (airport.Profile.Season == Weather.Season.Summer)
                if (isSummer) return 1.5;
                else return 0.5;
            if (airport.Profile.Season == Weather.Season.Winter)
                if (isSummer) return 0.5;
                else return 1.5;

            return 1;
        }
        //finds if there is a route for a passenger from one destination to another
        public static List<Airport> FindPassengerRoute(Airport airport, Passenger passenger)
        {
            Airport destination = passenger.Destination;

            PassengerRouteFinder finder = new PassengerRouteFinder(Airports.GetAirports());
            finder.calculateDistance(airport);

            return finder.getPathTo(passenger.Destination);
        }
       
        //returns the suggested passenger price for a route on a airliner
        public static double GetPassengerPrice(Airport dest1, Airport dest2)
        {

            double fuelConsumption = AirlinerTypes.GetTypes().Max(t => t.FuelConsumption);
            double groundTaxPerPassenger = 5;

            double tax = groundTaxPerPassenger;

            if (dest1.Profile.Country.Name != dest2.Profile.Country.Name)
                tax *= 2;

            double dist = MathHelpers.GetDistance(dest1.Profile.Coordinates, dest2.Profile.Coordinates);

            double fuel = GameObject.GetInstance().FuelPrice * dist * fuelConsumption;

            double expenses = GameObject.GetInstance().FuelPrice * dist * fuelConsumption + (dest2.getLandingFee() + dest1.getLandingFee()) / (2 * 100) + tax;

            return expenses * 2.5;

        }
        //returns the destination for a passenger
        public static Airport GetPassengerDestination(Passenger passenger, Airport airport)
        {
            List<Airport> airports = Airports.GetAirports();

            Dictionary<Airport, int> airportsList = new Dictionary<Airport, int>();
            airports.ForEach(a => airportsList.Add(a, (int)a.Profile.Size));

            return AIHelpers.GetRandomItem(airportsList);

        }
        /*! creates a number of passengers
        */
        public static void CreatePassengers(int passengers)
        {
            List<Airport> airports = Airports.GetAirports();
    
            foreach (Airport airport in Airports.GetAirports())
            {
  
                for (int i = 0; i < passengers; i++)
                {
                    double factor = (((int)airport.Profile.Size) + 1) * rnd.Next(1,20);
                    factor *= GetSeasonFactor(airport);

                    Passenger.PassengerType[] passengerTypes = (Passenger.PassengerType[]) Enum.GetValues(typeof(Passenger.PassengerType));
                    Passenger.PassengerType passengerType = passengerTypes[rnd.Next(0,passengerTypes.Length)];

                    List<AirlinerClass.ClassType> classTypes = new List<AirlinerClass.ClassType>();
                    
                    for (int j = 0; j < 20; j++)
                        classTypes.Add(AirlinerClass.ClassType.Economy_Class);
                    classTypes.Add(AirlinerClass.ClassType.Business_Class);
                    classTypes.Add(AirlinerClass.ClassType.Business_Class);
                    classTypes.Add(AirlinerClass.ClassType.First_Class);
                   
                    AirlinerClass.ClassType classType = classTypes[rnd.Next(0, classTypes.Count)];

                    Guid guid = Guid.NewGuid();
                 
                    Passenger passenger = new Passenger(guid.ToString(), passengerType, airport,classType); 
                    passenger.Updated = GameObject.GetInstance().GameTime;
                    passenger.Destination = GetRandomDestination(airport);
                    passenger.Factor = (int)factor;
                    passenger.Route = FindPassengerRoute(passenger.HomeAirport, passenger);

                    Passengers.AddPassenger(passenger);
                    airport.addPassenger(passenger);

                }
            }

           
        }
        //updates all passengers
        public static void UpdatePassengers()
        {
            //find new routes for all passengers not updated in the last week
            foreach (Passenger passenger in Passengers.GetPassengers().FindAll(p => p.Updated.AddDays(7) < GameObject.GetInstance().GameTime))
            {
                passenger.Updated = GameObject.GetInstance().GameTime;
                passenger.Destination = GetRandomDestination(passenger.CurrentAirport);
                passenger.Route = FindPassengerRoute(passenger.CurrentAirport,passenger);
                
            }
            //finds all passengers where the route is not set
            foreach (Passenger passenger in Passengers.GetPassengers().FindAll(p => p.Route.Count < 2))
            {
                passenger.Route = FindPassengerRoute(passenger.CurrentAirport, passenger);

                if (passenger.Route.Count > 1) passenger.Updated = GameObject.GetInstance().GameTime;
            }

        }
    }
}