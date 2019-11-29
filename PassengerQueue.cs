using System.Collections.Concurrent;
using System.Linq;
using System;

namespace ElevatorSimulator {

sealed class PassengerQueue {
    private static ConcurrentDictionary<Direction, ConcurrentBag<Passenger>> passengers
        = new ConcurrentDictionary<Direction, ConcurrentBag<Passenger>>(); 
    private static ConcurrentBag<Elevator> elevators = new ConcurrentBag<Elevator>();
    
    private PassengerQueue() {
    }

    public static int Count() {
        return passengers.Sum(e => e.Value.Count);
    }

    public static int Count(Direction direction) {
        return passengers
            .Where(e => e.Key.Equals(direction))
            .Sum(e => e.Value.Count);
    }

    public static void AddElevator(Elevator elevator) {
        elevators.Add(elevator);
        Console.WriteLine("+" + elevator + " is IN Service.");
    }

    public static void RemoveElevator(Elevator elevator) {
        elevators.TryTake(out elevator);
        Console.WriteLine("-" + elevator + " is OUT OFF Service.");
    }

    private static int Min(ConcurrentBag<Passenger> bag, int floor) {
        if (bag == null || bag.IsEmpty)
            return Int16.MaxValue;
        return bag.Min(p => Math.Abs(p.Floor - floor));
    }

    public static Passenger First(Elevator e) {
        lock(passengers) {
            int minDist = 0;
            Passenger passenger = null;
            if (e.State == Direction.IDLE) {
                minDist = passengers.Min(kv => Min(kv.Value, e.CurrentFloor));
                if (minDist == Int16.MaxValue)
                    return null;
                passenger = passengers
                    .First(kv => Min(kv.Value, e.CurrentFloor) == minDist)
                    .Value.First(p => Math.Abs(p.Floor-e.CurrentFloor) == minDist);
            } else { 
                minDist = passengers.Single(kv => kv.Key.Equals(e.State))
                    .Value.Min(p => Math.Abs(p.Floor - e.CurrentFloor));
                passenger = passengers.Single(kv => kv.Key.Equals(e.State))
                    .Value.First(p => Math.Abs(p.Floor-e.CurrentFloor) == minDist);
            }
            if (null == passenger)
                return null;
            Console.WriteLine("*" + passenger + " will get in " + e);
            if (passengers.Single(kv => kv.Key.Equals(passenger.Direction))
                .Value.TryTake(out passenger)) {
                    if (e.GetOn(passenger))
                        return passenger;
                    // NO-ROOM, ok, get back in the queue
                    GetOn(passenger);
                    return null;
                }
            return null;
        }
    }

    private static bool IsUp(Elevator e, Passenger p) {
        return e.State == Direction.UP &&   // elevator is going up
            p.Direction == e.State &&       // so the passenger is
            p.Floor > e.CurrentFloor;       // are you on my way up?
    }

    private static bool IsDown(Elevator e, Passenger p) {
        return e.State == Direction.DOWN && // elevator is going down
            p.Direction == e.State &&       // so the passenger is
            p.Floor < e.CurrentFloor;       // are you on my way down?
    }

    private static bool IsSameDir(Elevator e, Passenger p) {
        return IsUp(e, p) || IsDown(e, p) || e.State == Direction.IDLE;
    }

    public static void GetOn(Passenger passenger) {   
        if (elevators.Count() == 0) {
            Console.WriteLine("Elevators are OUT OFF Service!");
            return;
        }
        passengers
            .GetOrAdd(passenger.Direction, b => new ConcurrentBag<Passenger>())
            .Add(passenger);
        Console.WriteLine("+" + passenger + " is waiting for service...");
    }

}
}
