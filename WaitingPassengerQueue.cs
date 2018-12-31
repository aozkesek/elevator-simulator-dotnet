using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System;

namespace ElevatorSimulator {

        class WaitingPassengerQueue {
                private Object lockObject;
                private ConcurrentDictionary<int, Passenger> waitingPassengers;

                public WaitingPassengerQueue() {
                        lockObject = new Object();
                        waitingPassengers = 
                                new ConcurrentDictionary<int, Passenger>();
                }

                public bool Add(Passenger passenger) {
                        Console.WriteLine(passenger + " is queued.");
                        return waitingPassengers
                                .TryAdd(passenger.GetHashCode(), passenger);
                }

                public bool Remove(Passenger passenger) {
                        return waitingPassengers
                                .TryRemove(passenger.GetHashCode(), out passenger);
                } 

                public bool IsEmpty {
                        get { return waitingPassengers.IsEmpty; }
                }

                public int Count {
                        get { return waitingPassengers.Count; }
                }

                public Passenger GetFirst(Elevator elevator) {
                        
                        Monitor.Enter(lockObject);
                        try {
                                KeyValuePair<int, Passenger> kv = 
                                        waitingPassengers.AsParallel()
                                                .Where(p => p.Value.ElevatorId == -1)
                                                .First();

                                if (default(KeyValuePair<int, Passenger>).Equals(kv))
                                        return null;

                                Console.WriteLine("!" + elevator 
                                        + " is got " + kv.Value);
                                
                                kv.Value.ElevatorId = elevator.ElevatorId;
                                return kv.Value;

                        } 
                        catch(InvalidOperationException e) {
                                return null;
                        }
                        finally {
                                Monitor.Exit(lockObject);
                        }
                         
                }

                public void Get(Elevator elevator) {
                        
                        Monitor.Enter(lockObject);
                        try {
                                waitingPassengers.AsParallel()
                                .Where(kv => {
                                        Passenger p = kv.Value;
                                        return p.ElevatorId == -1 && 
                                                p.Direction == elevator.State &&
                                                ((p.Floor > elevator.CurrentFloor 
                                                && p.Direction == Direction.UP) || 
                                                (p.Floor < elevator.CurrentFloor 
                                                && p.Direction == Direction.DOWN));
                                }).ForAll(kv => {
                                        Console.WriteLine("!" + elevator 
                                                + " is got " + kv.Value);
                                        kv.Value.ElevatorId = elevator.ElevatorId;
                                        });
                        } finally {
                                Monitor.Exit(lockObject);
                        }
                
                }

                public bool IsPassengerWaitingFor(Direction destDirection, int floor) {
                        return waitingPassengers.AsParallel()
                                        .Where(p => 
                                                p.Value.Floor < floor && destDirection == Direction.DOWN || 
                                                p.Value.Floor > floor && destDirection == Direction.UP)
                                        .Count() > 0;
                }

                public void Pickup(Elevator elevator, Direction destDirection) {
                        waitingPassengers.AsParallel()
                                .Where((p) => p.Value.Floor == elevator.CurrentFloor 
                                        && p.Value.Direction == destDirection)
                                .ForAll((p) => {
                                        if (!elevator.IsAvailableRoom)
                                                return;
                                        
                                        Passenger pv = p.Value;
                                        if (!waitingPassengers.TryRemove(p.Key, out pv))
                                                return;

                                        Console.WriteLine(">>> " + pv + " is getting into " + elevator);
                                        elevator.AddPassenger(p.Value);
                                });
                }

        }
}