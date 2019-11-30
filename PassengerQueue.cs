using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ElevatorSimulator {

sealed class PassengerQueue {
    private static ConcurrentDictionary<Direction, ConcurrentBag<Passenger>> passengers
        = new ConcurrentDictionary<Direction, ConcurrentBag<Passenger>>(); 
    private static BlockingCollection<Elevator> elevators = new BlockingCollection<Elevator>();
    
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
        elevators.TakeWhile(e => e.Equals(elevator));
        Console.WriteLine("-" + elevator + " is OUT OFF Service.");
    }

    private static int Min(ConcurrentBag<Passenger> bag, int floor) {
        if (bag == null || bag.IsEmpty)
            return Int16.MaxValue;
        return bag.Min(p => Math.Abs(p.Floor - floor));
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
        Elevator elevator = Nearest(passenger);
        if (null != elevator) {
            if (elevator.WaitFor(passenger)) {
                Console.WriteLine("+" + passenger + " will get in " + elevator);
                return;
            }
        }
        passengers
            .GetOrAdd(passenger.Direction, b => new ConcurrentBag<Passenger>())
            .Add(passenger);

        Console.WriteLine("+" + passenger + " is waiting for service...");

    }

    private static Elevator Nearest(Passenger p) {
        IEnumerable<Elevator> availables = elevators.Where(e => e.IsRoomAvailable);
        if (null == availables || availables.Count() == 0)
            return null;

        // it must be at least 2 floor difference between elevator and passenger
        IEnumerable<Elevator> nearest = availables.Where(e => {
            if (e.State == p.Direction) {
                if (e.State == Direction.UP && e.CurrentFloor > p.Floor + 1)
                    return false;
                if (e.State == Direction.DOWN && e.CurrentFloor < p.Floor - 1)
                    return false;
                return true;
            } else if (e.State == Direction.IDLE)
                return true;
            
            return false;
        });

        if (null == nearest || nearest.Count() == 0)
            return null;

        int min = nearest.Min(e => Math.Abs(e.CurrentFloor - p.Floor));
        return nearest.First(e => Math.Abs(e.CurrentFloor - p.Floor) == min);
    }

}
}
