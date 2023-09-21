using System;
using System.Collections.Generic;
using System.Text;
using warehouse.Persistence;
using System.Linq;

namespace warehouse.Model
{
    public class SimulationModel
    {
        #region Properties
        private SimulationMap _map;
        private bool _isEnded;
        private ISimulationPersistence _dataAccess;
        
        private Int32 _time;
        private Int32 _steps;

        private Int32 _numRobots;
        private Int32 _numPods;
        private Int32 _numDocks;
        private Int32 _numShelves;

        private List<Pod> _selectedPods;
        private bool _isPodSelected;
        private Tuple<int, int> _uprightPod;

        private Tuple<Boolean, string> _clicked;
        
        public Int32 SimulationTime { get { return _time; } }
        public int NumRobots { get { return _numRobots; } }
        public int NumPods { get { return _numPods; } }
        public int NumDocks { get { return _numDocks; } }
        public int NumTartgetStations { get { return _numShelves; } }

        public Int32 Steps { get { return _steps; } }

        public bool isPodSelected { get { return _isPodSelected; } set { _isPodSelected = value; } }

        public SimulationMap Map { get { return _map; } }
        #endregion

        #region Events

        public event EventHandler<SimulationEventArgs> RobotAdded; // ezekre nincs még rendes
        public event EventHandler<SimulationEventArgs> PodAdded; //
        public event EventHandler<SimulationEventArgs> DockAdded; //
        public event EventHandler<SimulationEventArgs> ShelfAdded; //
        public event EventHandler<SimulationEventArgs> RobotCharged;
        public event EventHandler<SimulationEventArgs> Delivered;
        public event EventHandler<SimulationEventArgs> Pickup;
        public event EventHandler<SimulationEventArgs> SimulationStarted; //
        public event EventHandler<SimulationEventArgs> SpeedChanged; //
        public event EventHandler<SimulationEventArgs> TimePassed;
        public event EventHandler<SimulationEventArgs> SimulationEnded;
        public event EventHandler<SimulationEventArgs> RobotDeleted;
        public event EventHandler<SimulationEventArgs> PodDeleted;
        public event EventHandler<SimulationEventArgs> DockDeleted;
        public event EventHandler<SimulationEventArgs> ShelfDeleted;
        public event EventHandler<SimulationEventArgs> PodSelected; //
        public event EventHandler<SimulationEventArgs> PodReplaced;//
        public event EventHandler<SimulationEventArgs> ProductAdded; //







        #endregion

        #region Public methods (events)

        //na ezek nem tudom mit csinálnak de kellenek, remélem így jók \_(u_u)_/
        public void AdvanceTime() 
        {
            if (_isEnded) // ha már vége, nem folytathatjuk
                return;

            _time++;
            OnSimulationAdvanced();

        }

        public void OnSimulationAdvanced()
        {
            if (TimePassed != null)
                TimePassed(this, new SimulationEventArgs(false,_steps,_time));
        }
        private void OnSimulationEnded(Boolean isEnded)
        {
            if (SimulationEnded != null)
                SimulationEnded(this, new SimulationEventArgs(isEnded, _steps, _time));
        }

        private void OnRobotCharged()
        {
            if (RobotCharged != null)
                RobotCharged(this, new SimulationEventArgs(false, _steps, _time));

        }

        private void OnDelivered()
        {
            if(Delivered != null)
                Delivered(this, new SimulationEventArgs(false, _steps, _time));
        }

        public void OnPickUp()
        {
            if(Pickup != null)
                Pickup(this, new SimulationEventArgs(false, _steps, _time));
        }



        #endregion

        #region Constructor

        public SimulationModel(ISimulationPersistence dataAccess, int length, int width, string name)
        {
            _dataAccess = dataAccess;
            _map = new SimulationMap(length, width, name);
            _time = 0;
            _steps = 0;
            
            _numPods = 0;
            _numRobots = 0;
            _numShelves = 0;
            _numDocks = 0;
        }

        #endregion

        #region Advancing methods

        public bool isEnded()
        {
            bool ended= true;

            foreach (Pod p in _map.Pods)
            {
                if (p.Goods.Any())
                {
                    ended = false;
                }
            }
            
            foreach(Robot r in _map.Robots)
            {
                if (r.hasPod)
                {
                    ended = false;
                }
            }

            return ended;
        }

        public void NewSimulation(int length, int width, string name)
        {
            _map = new SimulationMap(length, width, name);
            
            _time = 0;
            _steps = 0;

            _numPods = 0;
            _numRobots = 0;
            _numShelves = 0;
            _numDocks = 0;
        }
        public bool CanMoveForward(Robot r)
        {
            if(r.Charge == 0)
            { 
                return false;
            }


            switch (r.RobotDirection)
            {
                case Direction.East:
                    bool x = _map.IntMap[r.Position.Item1 + 1, r.Position.Item2] == 0;
                    bool y = r.Position.Item1 !> _map.Width;
                    return x && y;
                case Direction.West:
                    x = _map.IntMap[r.Position.Item1 - 1, r.Position.Item2] == 0; //szabad a mező
                    y = r.Position.Item1 >=0 ; //nem map széle
                    return x && y;
                case Direction.North:
                    x= _map.IntMap[r.Position.Item1, r.Position.Item2 + 1] == 0;
                    y = r.Position.Item2 !> _map.Width;
                    return x && y;
                case Direction.South:
                    x = _map.IntMap[r.Position.Item1, r.Position.Item2 - 1] == 0;
                    y = r.Position.Item2 > 0;
                    return x && y;

            }

            return false;
        }

        public void MoveForward(Robot r)
        {
            if (CanMoveForward(r)) //tud-e menni az irányba
            {
                _map.IntMap[r.Position.Item1, r.Position.Item2] = 0;

                switch (r.RobotDirection)
                {
                    case Direction.East:
                        _map.IntMap[r.Position.Item1 + 1, r.Position.Item2] = 2;
                        r.Position = Tuple.Create(r.Position.Item1 + 1, r.Position.Item2);
                        break;
                    case Direction.West:
                        _map.IntMap[r.Position.Item1 - 1, r.Position.Item2] = 2;
                        r.Position = Tuple.Create(r.Position.Item1 - 1, r.Position.Item2);
                        break;
                    case Direction.North:
                        _map.IntMap[r.Position.Item1, r.Position.Item2 + 1] = 2;
                        r.Position = Tuple.Create(r.Position.Item1, r.Position.Item2 + 1);
                        break;
                    case Direction.South:
                        _map.IntMap[r.Position.Item1, r.Position.Item2 - 1] = 2;
                        r.Position = Tuple.Create(r.Position.Item1 - 1, r.Position.Item2);
                        break;
                }

            }

            r.Charge--;
        }

        public void Turn(Robot r, Direction d)
        {
            r.RobotDirection = d;
            r.Charge--;
        }

        public void LiftPod(Robot r, Pod p)
        {
            r.hasPod=true;
            r.getPod= p;
            p.Owner = r;
            _map.IntMap[p.Position.Item1, p.Position.Item2] = 0;
        }

        public void Delivery(Robot r, Shelf t)
        {
            r.hasPod = false;
            t.Assigned = false;
            OnDelivered();
            //AdvanceTime();
        }

        #endregion

        #region Path finding methods


        //ez jó, ha üres a robot
        //ez most így először végig megy az x tengelyen, aztán az y tengelyen egy nagy derékszögben, collisiont nem figyel, de asszem azt majd nem itt kell
        public List<Tuple<int,int, Direction>> FindPath(Robot r, Tuple<int,int> t)
        {
           int x=r.Position.Item1;
           int y=r.Position.Item2;

           int ax=t.Item1;
           int ay=t.Item2;

           int lengthx=Math.Abs(x-ax);
           int lengthy = Math.Abs(y - ay);

           List<Tuple<int, int, Direction>> coordinates = new List<Tuple<int, int, Direction>>();


            if (x > ax) //jobbra van a robot a podtól
            {
                for(int i=0; i<lengthx; i++)
                {
                    coordinates.Add(Tuple.Create(x - 1, y, Direction.East));
                }
            }
            else //balra van a robot a podtól
            {
                for (int i = 0; i < lengthx; i++)
                {
                    coordinates.Add(Tuple.Create(x + 1, y, Direction.West));
                }
            }

            if (y > ay) //lefele van a pod a robbottól
            {
                for(int i=0; i<lengthy; i++)
                {
                    coordinates.Add(Tuple.Create(ax,y - 1, Direction.South));
                }
            }
            else //felfele van a pod a robbottól
            {
                for (int i = 0; i < lengthy; i++)
                {
                    coordinates.Add(Tuple.Create(ax, y + 1, Direction.North));
                }
            }
           
            return coordinates;
       }

        //TODO: el innen xD
         public void ExecutePath(Robot r, List<Tuple<int, int, Direction>> c) 
        {
            for(int i=0; i<c.Count; i++)
            {
                if(r.RobotDirection != c[i].Item3)
                {
                    Turn(r, c[i].Item3);
                    //AdvanceTime();

                }

                MoveForward(r);
                //AdvanceTime();
                
            }

        }


        // minden 10% egy időegység + maradék is egy időegység
        //valszeg Tomi nem így gondolta
        public void Charge(Robot r, Dock cs) 
        {

            cs.stationedRobot = r;

            int chargeunits;

            if (r.Charge % 5 == 0)
            {
                chargeunits = (r.Charge - (r.Charge % 5)) / 5;
            }
            else 
            {
                chargeunits = (r.Charge - (r.Charge % 5)) / 5 + 1;
            }

            for (int i=0; i < chargeunits-1; i++)
            {
                r.Charge += 10;
                //AdvanceTime();
            }

            r.Charge = 100;
            //AdvanceTime();

            OnRobotCharged();

        }

        public List<Tuple<int, int, Direction>> PlanDeliveryPath(Robot r, Tuple<int,int> t)
        {
            List<Tuple<int, int, Direction>> coordinates = new List<Tuple<int, int, Direction>>();

            int x = r.Position.Item1;
            int y = r.Position.Item2;

            int ax = t.Item1;
            int ay = t.Item2;

            int lengthx = Math.Abs(x - ax);
            int lengthy = Math.Abs(y - ay);

            //ebben már kerülgetni kell
            return coordinates;
        }

        public Tuple<int,int> WhichStation(Robot r) //azert ne stationt adjon hogy biztosan tudjunk returnolni valamit? (mi van ha egyik targetnek se kell ami van a robotnal veletlen, ne dobjon hibat + every path returns)
        {
            Tuple<int, int> coordinates = Tuple.Create(r.Position.Item1, r.Position.Item2); //ha nincs megfelelő station, a robot koordinátáit fogja visszaadni
            if (!r.hasPod) { /*throw exception*/ }
            foreach(Shelf t in _map.Shelves)
            {
                if (r.getPod.Goods.Contains(t.ProductToTake)&&!t.Assigned) //akkor menjen oda ha oda kell a roboton lévő item és más robot nem megy még oda
                {
                    coordinates = Tuple.Create(t.Position.Item1, t.Position.Item2);
                    break;
                }
            }

            return coordinates;
        }

        public Tuple<int,int> WhichPod(Robot r)
        {
            if(r.hasPod) { /*throw exception*/}
            Tuple<int, int> coordinates = Tuple.Create(r.Position.Item1, r.Position.Item2);
            int distance = 1000;

            int tmp;

            foreach(Pod p in _map.Pods)
            {
                tmp = Math.Abs(r.Position.Item1 - p.Position.Item1) + Math.Abs(r.Position.Item2 - p.Position.Item2);
                if (tmp < distance)
                {
                    distance = tmp;
                    coordinates = Tuple.Create(p.Position.Item1, p.Position.Item2);
                }
            }

            return coordinates;
        }

        public Tuple<int, int> GetClosestDock(Robot r) 
        {
            int distance = 1000;
            Tuple<int, int> coordinates = Tuple.Create(r.Position.Item1, r.Position.Item2);
            int tmp;

            foreach(Dock c in _map.Docks)
            {
                tmp = Math.Abs(r.Position.Item1 - c.Position.Item1) + Math.Abs(r.Position.Item2 - c.Position.Item2);
                if (tmp < distance)
                {
                    distance = tmp;
                    coordinates = Tuple.Create(c.Position.Item1, c.Position.Item2);

                }
            }

            //charging station assigned

            return coordinates;

        }

        #endregion

        #region Public szerkesztő methodok - szerintem ezek nem a simulationmodelbe kellenek de megírom egyelőre ide őket, külön model kell a szerkesztőnek


        public void AddRobot(int x, int y)
        {

            if (_map.IntMap[x, y] != 0) { return; }
            Robot r = new Robot(Tuple.Create(x,y), _numRobots + 1);
            bool robi =_map.Add(r);
            _numRobots++;
            OnRobotAdded();
        }

        public void AddPod(int x, int y)
        {
            if (_map.IntMap[x, y] != 0) { return; }

            Pod p = new Pod(Tuple.Create(x, y));
            _map.Add(p);
            _numPods++;
            OnPodAdded();
        }

        public void AddDock(int x, int y)
        {
            if (_map.IntMap[x, y] !=0 ) { return; }

            Dock c = new Dock(Tuple.Create(x, y));
            _map.Add(c);
            _numDocks++;
            OnDockAdded();
        }
        public void AddShelf(int x, int y)
        {

            if (_map.IntMap[x, y] !=0 ) { return; }
            
            //TODO: mit kap
            Shelf t = new Shelf(Tuple.Create(x, y), _numShelves);
            _map.Add(t);
            _numShelves++;
            OnShelfAdded();
        }
        public void Delete(int x, int y)
        {
            Tuple<int, int> coords = Tuple.Create(x, y);
            switch (_map.IntMap[x, y])
            {
                case 0: 
                    return;
                case 1: //robot
                    foreach(Robot r in _map.Robots)
                    {
                        if (coords.Equals(r.Position)){
                            _map.Remove(r);
                            _numRobots--;
                            OnRobotDeleted();
                            return;
                        }
                    }
                    return; 
                case 2: //pod
                    foreach (Pod p in _map.Pods)
                    {
                        if (coords.Equals(p.Position)){
                            _map.Remove(p);
                            _numPods--;
                            OnPodDeleted();
                            return;
                        }
                    }
                    return;
                case 3: //shelf
                    foreach (Shelf s in _map.Shelves)
                    {
                        if (coords.Equals(s.Position)){
                            _map.Remove(s);
                            _numShelves--;
                            OnPodDeleted();
                            return;

                        }
                    }
                    return;
                case 4: //chst
                    foreach (Dock d in _map.Docks)
                    {
                        if (coords.Equals(d.Position)){
                            _map.Remove(d);
                            _numShelves--;
                            OnDockDeleted();
                            return;

                        }
                    }
                    return;


            }

        }

        public void Select(int x, int y)
        {
            Tuple<int, int> coords = Tuple.Create(x, y);

            if(_map.IntMap[x, y] == 3)
            {
                if (!_isPodSelected)
                {
                    _selectedPods = new List<Pod>();
                    _isPodSelected = true;
                }

                foreach (Pod s in _map.Pods)
                {
                    if (coords.Equals(s.Position))
                    {
                        _selectedPods.Add(s);

                    }
                }
            }

            OnPodSelected();

        }

        public void Replace(int x, int y)
        {
            if (_map.IntMap[x, y] != 0)
            {
                return; //exception?
            }

            Tuple<int, int> coords = Tuple.Create(x, y);

            if (_isPodSelected) {

                Tuple<int, int> distance = Tuple.Create(coords.Item1 - _uprightPod.Item1, coords.Item2 - _uprightPod.Item2);
                bool canbemoved = true;
                foreach (Pod s in _selectedPods)
                {
                    int c1 = s.Position.Item1 + distance.Item1;
                    int c2 = s.Position.Item2 + distance.Item2;
                    if (c1 < 0 || c2 < 0 || c1 >= _map.Width || c2 >= _map.Height || _map.IntMap[c1,c2]!=0)
                    {
                        canbemoved = false;
                    }


                }

                if (canbemoved)
                {
                    foreach (Pod s1 in _map.Pods)
                    {
                        foreach (Pod s2 in _selectedPods)
                        {
                            if (s1.Position.Equals(s2.Position))
                            {
                                _map.IntMap[s1.Position.Item1, s1.Position.Item2] = 0;
                                _map.IntMap[s1.Position.Item1 + distance.Item1, s1.Position.Item2 + distance.Item2] = 3;
                                s1.Position = Tuple.Create(s1.Position.Item1 + distance.Item1, s1.Position.Item2 + distance.Item2);
                            }
                        }

                    }

                }

                OnPodReplaced();

            }


            
        }


        public void AddProduct(List<int> products) {

            if (isPodSelected)
            {
                foreach (Pod s1 in _map.Pods)
                {
                    foreach (Pod s2 in _selectedPods)
                    {
                        if (s1.Position.Equals(s2.Position))
                        {
                            foreach(int p in products)
                            {
                                s1.Goods.Add(p);
                            }
                        }
                    }

                }

            }

            OnProductsAdded();
        }



        #endregion


        //nem kellene egy following step minden robotnak? hogy tudjuk a timeot egyszerre advanceolni



        //ez nem értem pontosan mire lesz jó ha már van canmoveforward?? mi lesz ha collideolnanak? egy secet késleltetjük a lépést?
        /*public bool AreColliding()
        {

        

        }*/

       

        #region Private event methods


        private void OnRobotAdded()
        {
            if (RobotAdded != null)
                RobotAdded(this, new SimulationEventArgs(_isEnded, _steps, _time));
        }

        private void OnPodAdded()
        {
            if (PodAdded != null)
                PodAdded(this, new SimulationEventArgs(_isEnded, _steps, _time));
        }

        
        private void OnDockAdded()
        {
            if (DockAdded != null)
                DockAdded(this, new SimulationEventArgs(_isEnded, _steps, _time));
        }

        private void OnShelfAdded()
        {
            if (ShelfAdded != null)
                ShelfAdded(this, new SimulationEventArgs(_isEnded, _steps, _time));
        }

        private void OnRobotDeleted()
        {
            if (RobotDeleted != null)
                RobotAdded(this, new SimulationEventArgs(_isEnded, _steps, _time));
        }
        private void OnPodDeleted()
        {
            if (PodDeleted != null)
                PodDeleted(this, new SimulationEventArgs(_isEnded, _steps, _time));
        }

        private void OnShelfDeleted()
        {
            if (ShelfDeleted != null)
                ShelfDeleted(this, new SimulationEventArgs(_isEnded, _steps, _time));
        }

        private void OnDockDeleted()
        {
            if (DockDeleted != null)
                DockDeleted(this, new SimulationEventArgs(_isEnded, _steps, _time));
        }

        private void OnPodSelected()
        {
            if (PodSelected != null)
                PodSelected(this, new SimulationEventArgs(_isEnded, _steps, _time));

        }

        private void OnPodReplaced()
        {
            if (PodReplaced != null)
                PodReplaced(this, new SimulationEventArgs(_isEnded, _steps, _time));
        }


        private void OnProductsAdded()
        {
            if (ProductAdded != null)
                ProductAdded(this, new SimulationEventArgs(_isEnded, _steps, _time));
        }


        private void OnSimulationStarted()
        {

        }

        private void OnSpeedChanged()
        {

        }


        #endregion

    }




}
