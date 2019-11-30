using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ElevatorSimulator {

sealed class PassengerQueue {
    private static ConcurrentDictionary<Direction, ConcurrentBag<Passenger>> 
        passengers = new ConcurrentDictionary<Direction, ConcurrentBag<Passenger>>(); 
    private static ConcurrentDictionary<int, Elevator> 
        elevators = new ConcurrentDictionary<int, Elevator>();
    
    private PassengerQueue() {
    }

    public static void AddElevator(Elevator elevator) {
        if (elevators.TryAdd(elevator.GetHashCode(), elevator))
            Console.WriteLine("+" + elevator + " is IN Service.");
    }

    public static void RemoveElevator(Elevator elevator) {
        if (elevators.TryRemove(elevator.GetHashCode(), out elevator))
            Console.WriteLine("-" + elevator + " is OUT OFF Service.");
    }

    public static void GetOn(Passenger passenger) {   
        
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
        IEnumerable<KeyValuePair<int, Elevator>> availables = 
            elevators.Where(e => e.Value.IsRoomAvailable);
        if (null == availables || availables.Count() == 0)
            return null;

        // it must be at least 2 floor difference between elevator and passenger
        IEnumerable<KeyValuePair<int, Elevator>> nearest = availables.Where(e => {
            if (e.Value.State == p.Direction) {
                if (e.Value.State == Direction.UP && e.Value.CurrentFloor > p.Floor + 1)
                    return false;
                if (e.Value.State == Direction.DOWN && e.Value.CurrentFloor < p.Floor - 1)
                    return false;
                return true;
            } else if (e.Value.State == Direction.IDLE)
                return true;
            
            return false;
        });

        if (null == nearest || nearest.Count() == 0)
            return null;

        int min = nearest.Min(e => Math.Abs(e.Value.CurrentFloor - p.Floor));
        KeyValuePair<int, Elevator> first = nearest.First(e => Math.Abs(e.Value.CurrentFloor - p.Floor) == min);
        return first.Value;

    }

}
}
