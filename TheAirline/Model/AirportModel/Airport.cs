﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheAirline.Model.AirlineModel;
using TheAirline.Model.GeneralModel;
using TheAirline.Model.GeneralModel.StatisticsModel;
using TheAirline.Model.AirlinerModel;
using TheAirline.Model.PassengerModel;


namespace TheAirline.Model.AirportModel
{
    //the class for an airport
    public class Airport
    {
        public AirportProfile Profile { get; set; }
        private List<Passenger> Passengers;
        private List<AirlineAirportFacility> Facilities;
        public AirportStatistics Statistics { get; set; }
        //public Dictionary<Airline, Dictionary<AirportFacility.FacilityType, AirportFacility>> Facilities { get; private set; }
        public Weather Weather { get; set; }
        public List<Runway> Runways { get; set; }
        public Terminals Terminals { get; set; }
        public List<Hub> Hubs { get; set; }
        public Boolean IsHub { get { return this.Hubs.Count > 0; } set { ;} }
        public Airport(AirportProfile profile)
        {
            this.Profile = profile;
            this.Passengers = new List<Passenger>();
            //this.Facilities = new Dictionary<Airline, Dictionary<AirportFacility.FacilityType, AirportFacility>>();
            this.Facilities = new List<AirlineAirportFacility>();
            this.Statistics = new AirportStatistics();
            this.Weather = new Weather();
            this.Terminals = new Terminals(this);
            this.Runways = new List<Runway>();
            this.Hubs = new List<Hub>();
          

         }
        //returns the maximum value for the run ways
        public long getMaxRunwayLength()
        {
            var query = from r in this.Runways
                        select r.Length;
            return query.Max();
       

        }
        
        //clears the list of passengers
        public void clearPassengers()
        {
            this.Passengers = new List<Passenger>();
        }
       
        //returns all passengers at the airport
        public List<Passenger> getPassengers()
        {
            return this.Passengers;
        }
        //returns all passengers for a specific destination
        public List<Passenger> getPassengers(Airport destination)
        {
            return this.Passengers.FindAll(p => p.Destination == destination || p.getNextRouteDestination(this)==destination);
        }
        //removes a passenger from the airport
        public void removePassenger(Passenger passenger)
        {
            this.Passengers.Remove(passenger);
        }
        //adds a passenger to the airport
        public void addPassenger(Passenger passenger)
        {
            this.Passengers.Add(passenger);
        }
        
        //returns the price for a gate
        public long getGatePrice()
        {
            long sizeValue = 1000 + 1023 * ((int)this.Profile.Size + 1);
            return sizeValue;
        }
        //returns the fee for landing at the airport
        public double getLandingFee()
        {
            long sizeValue = 1543 * ((int)this.Profile.Size + 1);
            return sizeValue;
        }
        //sets a facility to an airline
        public void setAirportFacility(Airline airline, AirportFacility facility, DateTime finishedDate)
        {
            if (getAirlineAirportFacility(airline, facility.Type) == null)
            {
                this.Facilities.Add(new AirlineAirportFacility(airline, facility, finishedDate));
            }
            else
            {
                this.Facilities.Remove(getAirlineAirportFacility(airline, facility.Type));
                this.Facilities.Add(new AirlineAirportFacility(airline, facility, finishedDate));
       
            }
          
        }
      
        //returns the facility of a specific type for an airline
        public AirportFacility getAirportFacility(Airline airline, AirportFacility.FacilityType type)
        {
            return (from f in this.Facilities where f.Airline == airline && f.Facility.Type == type select f.Facility).FirstOrDefault(); 
        }
        //return the airport facility for a specific type for an airline
        public AirlineAirportFacility getAirlineAirportFacility(Airline airline, AirportFacility.FacilityType type)
        {
            return this.Facilities.Where(f => f.Airline == airline && f.Facility.Type == type).FirstOrDefault();
        }
        //return all the facilities for an airline
        public List<AirlineAirportFacility> getAirportFacilities(Airline airline)
        {
            return (from f in this.Facilities where f.Airline==airline select f).ToList();
           
        }
        //returns if an airline has any facilities at the airport
        public Boolean hasFacilities(Airline airline)
        {
            Boolean hasFacilities = false;
            foreach (AirportFacility.FacilityType type in Enum.GetValues(typeof(AirportFacility.FacilityType)))
            {
                if (getAirportFacility(airline, type).TypeLevel > 0)
                    hasFacilities = true;
            }
            return hasFacilities;
        }
        //returns if an airline has any airliners with the airport as home base
        public Boolean hasAsHomebase(Airline airline)
        {
            foreach (FleetAirliner airliner in airline.Fleet)
                if (airliner.Homebase == this)
                    return true;

            return false;
        }
        //downgrades the facility for a specific type for an airline
        public void downgradeFacility(Airline airline, AirportFacility.FacilityType type)
        {
            AirportFacility currentFacility = getAirportFacility(airline, type);

            List<AirportFacility> facilities = AirportFacilities.GetFacilities(type);

            facilities.Sort((delegate(AirportFacility f1, AirportFacility f2) { return f1.TypeLevel.CompareTo(f2.TypeLevel); }));

            int index = facilities.IndexOf(this.getAirportFacility(GameObject.GetInstance().HumanAirline, type));

            setAirportFacility(airline, facilities[index - 1],GameObject.GetInstance().GameTime);

        }
        //returns the price for a hub
        public long getHubPrice()
        {
            long price = 500000 + 250000 * ((int)this.Profile.Size);
            return price;
        }
        // chs, 2011-31-10 added for pricing of a terminal
        //returns the price for a terminal
        public long getTerminalPrice()
        {
            long price = 2000000 + 150000 * ((int)this.Profile.Size + 1);
            return price;
   
        }
        //returns the price for a gate at a bough terminal
        public long getTerminalGatePrice()
        {
            long price = 125000 * ((int)this.Profile.Size + 1);
            return price;
           
        }
        // chs, 2011-27-10 added for the possibility of purchasing a terminal
        //adds a terminal to the airport
        public void addTerminal(Terminal terminal)
        {
            this.Terminals.addTerminal(terminal);
        }
        //removes a terminal from the airport
        public void removeTerminal(Terminal terminal)
        {
            this.Terminals.removeTerminal(terminal);
        }
    }
    //the collection of airports
    public class Airports
    {
        private static Dictionary<string, Airport> airports = new Dictionary<string, Airport>();
        //clears the list
        public static void Clear()
        {
            airports = new Dictionary<string, Airport>();
        }
        //adds an airport
        public static void AddAirport(Airport airport)
        {
            airports.Add(airport.Profile.IATACode, airport);
        }
        //returns an airport based on iata code
        public static Airport GetAirport(string iata)
        {
            if (airports.ContainsKey(iata))
                return airports[iata];
            else
                return null;
        }
        //returns a possible match for coordinates
        public static Airport GetAirport(Coordinates coordinates)
        {
            foreach (Airport airport in GetAirports())
                if (airport.Profile.Coordinates.CompareTo(coordinates) == 0)
                    return airport;
            return null;
        }
        //returns all airports with a specific size
        public static List<Airport> GetAirports(AirportProfile.AirportSize size)
        {
         
            return GetAirports().FindAll((delegate(Airport airport) { return airport.Profile.Size == size; }));
          
        }
        //returns all airports from a specific country
        public static List<Airport> GetAirports(Country country)
        {
             return GetAirports().FindAll((delegate(Airport airport) { return airport.Profile.Country == country; }));
  
        }
        //returns all airports from a specific region
        public static List<Airport> GetAirports(Region region)
        {
             return GetAirports().FindAll((delegate(Airport airport) { return airport.Profile.Country.Region == region; }));
  
        }
        //returns the list of airports
        public static List<Airport> GetAirports()
        {
            return airports.Values.ToList();
        }
        //returns the total number of airports
        public static int Count()
        {
            return airports.Values.Count;
        }

    }
  
}
