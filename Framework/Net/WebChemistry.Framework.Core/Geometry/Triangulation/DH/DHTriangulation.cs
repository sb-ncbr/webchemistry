namespace WebChemistry.Framework.Geometry.Triangulation.DH
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Math;

    class DHTriangulation<T>
    {
        private List<Tetrahedron<T>> simplices;

        private DisconnectedFace<T>[] FacesBuffer = new DisconnectedFace<T>[1024];
        private TriangulationVertex<T> Infinity = new TriangulationVertex<T>();
        private TetrahedronFactory<T> tetrahedronFactory = new TetrahedronFactory<T>();
        private DisconnectedFaceFactory<T> faceFactory = new DisconnectedFaceFactory<T>();

        private DHTriangulation() { }

        public List<Tetrahedron<T>> Simplices { get { return simplices; } }

        /// <summary>
        /// creates a new Triangulation from the given sequence of vertices
        /// </summary>
        public static DHTriangulation<T> Create(IEnumerable<T> data, Func<T, Vector3D> positionSelector)
        {
            DHTriangulation<T> result = new DHTriangulation<T>();

            // Order vertices
            var ordered = HilbertOrdering.OrderPoints(data.Select((d, i) => new TriangulationVertex<T>(d, positionSelector(d), i)));

            result.simplices = new List<Tetrahedron<T>>(ordered.Length * 7);

            // Create first tetrahedron
            Tetrahedron<T> start = result.Init(ordered);

            // Incremental construction
            for (int i = 4; i < ordered.Length; i++)
            {
                var updateRegion = result.Localize(ordered[i], start);
                start = result.Insert(updateRegion, ordered[i]);
            }

            result.Finish(start);

            result.FacesBuffer = null;
            result.tetrahedronFactory = null;
            result.faceFactory = null;

            return result;
        }

        /// <summary>
        /// Fills the Simplices list
        /// </summary>
        private void Finish(Tetrahedron<T> start)
        {
            Tetrahedron<T> stack = start;
            start.Previous = null;
            start.Marked = true;

            while (stack != null)
            {
                var t = stack;
                stack = stack.Previous;
                simplices.Add(t);

                if (!t.N0.Marked)
                {
                    t.N0.Previous = stack;
                    stack = t.N0;
                    t.N0.Marked = true;
                }
                if (!t.N1.Marked)
                {
                    t.N1.Previous = stack;
                    stack = t.N1;
                    t.N1.Marked = true;
                }
                if (!t.N2.Marked)
                {
                    t.N2.Previous = stack;
                    stack = t.N2;
                    t.N2.Marked = true;
                }
                if (!t.N3.Marked)
                {
                    t.N3.Previous = stack;
                    stack = t.N3;
                    t.N3.Marked = true;
                }
            }
        }

        /// <summary>
        /// Creates initial tetrahedron
        /// </summary>
        private Tetrahedron<T> Init(TriangulationVertex<T>[] vertices)
        {
            //Create first tetrahedron
            Tetrahedron<T> first = tetrahedronFactory.Create(vertices[0], vertices[1], vertices[2], vertices[3], Infinity);
            first.SphereDeterminant(Infinity);

            // Degenerated shape - 4 coplanar points
            while (first.MinorsQ < 0.0000001)
            {
                Random rnd = new Random();
                int i = rnd.Next(4, vertices.Length);
                var temp = vertices[0];
                vertices[0] = vertices[i];
                vertices[i] = temp;
                tetrahedronFactory.Dispose(first);
                first = tetrahedronFactory.Create(vertices[0], vertices[1], vertices[2], vertices[3], Infinity);
                first.SphereDeterminant(Infinity);
            }

            //Create infinite cells
            Tetrahedron<T> cell1 = tetrahedronFactory.Create(first.V0, first.V1, first.V2, Infinity, first.V3);
            Tetrahedron<T> cell2 = tetrahedronFactory.Create(first.V1, first.V2, first.V3, Infinity, first.V0);
            Tetrahedron<T> cell3 = tetrahedronFactory.Create(first.V2, first.V3, first.V0, Infinity, first.V1);
            Tetrahedron<T> cell4 = tetrahedronFactory.Create(first.V3, first.V0, first.V1, Infinity, first.V2);

            // Link all cells
            cell1.N3 = first; cell2.N3 = first; cell3.N3 = first; cell4.N3 = first;
            first.N3 = cell1; first.N0 = cell2; first.N1 = cell3; first.N2 = cell4;
            cell1.N2 = cell4; cell1.N1 = cell3; cell1.N0 = cell2;
            cell2.N2 = cell1; cell2.N1 = cell4; cell2.N0 = cell3;
            cell3.N2 = cell2; cell3.N1 = cell1; cell3.N0 = cell4;
            cell4.N2 = cell3; cell4.N1 = cell2; cell4.N0 = cell1;

            return first;
        }

        /// <summary>
        /// Inserts a point into triangulation
        /// </summary>
        /// <param name="toProcess">A two-directional linked list of tetrahedra, whose circumsphere contains vertex</param>
        /// <param name="vertex">The new vertex</param>
        private Tetrahedron<T> Insert(Tetrahedron<T> toProcess, TriangulationVertex<T> vertex)
        {
            // This is an improved version of the cavity algorithm which works without removing all invalid tetrahedra.

            DisconnectedFace<T> innerFaces = null;
            Tetrahedron<T> result = null;
            Tetrahedron<T> toUnMark = null;

            while (toProcess != null)
            {
                Tetrahedron<T> current = toProcess;
                toProcess = current.Previous;
                if (toProcess != null) toProcess.Next = null;

                Tetrahedron<T> localStack = null;
                TriangulationVertex<T> oldVertex;

                if (current.N0 != null && !current.N0.Marked)
                {
                    oldVertex = current.V0;
                    current.V0 = vertex;
                }
                else if (current.N1 != null && !current.N1.Marked)
                {
                    oldVertex = current.V1;
                    current.V1 = vertex;
                }
                else if (current.N2 != null && !current.N2.Marked)
                {
                    oldVertex = current.V2;
                    current.V2 = vertex;
                }
                else if (current.N3 != null && !current.N3.Marked)
                {
                    oldVertex = current.V3;
                    current.V3 = vertex;
                }
                else // All neighbours are inner tetrahedrons - unlink and dispose
                {
                    if (current.N0 != null) current.N0.UpdateLink(current, null);
                    if (current.N1 != null) current.N1.UpdateLink(current, null);
                    if (current.N2 != null) current.N2.UpdateLink(current, null);
                    if (current.N3 != null) current.N3.UpdateLink(current, null);
                    tetrahedronFactory.Dispose(current);
                    continue;
                }
                current.VertexUpdated();
                current.Previous = null;
                localStack = current;
                current.LocalFlag = true;

                while (localStack != null)
                {
                    current = localStack;
                    localStack = current.Previous;
                    if (result == null && !current.Infinite) result = current;
                    current.Previous = toUnMark;
                    toUnMark = current;

                    if (vertex != current.V0)
                    {
                        if (current.N0 != null)
                        {
                            if (!current.N0.LocalFlag)
                            {
                                if (!current.N0.Marked)
                                {
                                    Tetrahedron<T> neighbour = current.N0;
                                    current.N0 = tetrahedronFactory.Create(current.V1, current.V2, current.V3, oldVertex, current.V0);

                                    current.N0.N3 = current;
                                    neighbour.UpdateLink(current, current.N0);
                                    if (result == null && !current.N0.Infinite) result = current.N0;
                                    if (current.N0.V0 == vertex)
                                    {
                                        current.N0.N0 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N0, 1);
                                        innerFaces = AddNewFace(innerFaces, current.N0, 2);
                                    }
                                    else if (current.N0.V1 == vertex)
                                    {
                                        current.N0.N1 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N0, 0);
                                        innerFaces = AddNewFace(innerFaces, current.N0, 2);
                                    }
                                    else if (current.N0.V2 == vertex)
                                    {
                                        current.N0.N2 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N0, 0);
                                        innerFaces = AddNewFace(innerFaces, current.N0, 1);
                                    }
                                }
                                else if (TryUpdate(current.N0, oldVertex, vertex))
                                {
                                    RemoveFromList(current.N0, ref toProcess);
                                    // Add to local stack
                                    current.N0.Previous = localStack;
                                    localStack = current.N0;
                                    current.N0.LocalFlag = true;
                                }
                                else
                                {
                                    // Disconnect
                                    current.N0.UpdateLink(current, null);
                                    innerFaces = AddNewFace(innerFaces, current, 0);
                                    current.N0 = null;
                                }
                            }
                        }
                        else
                        {
                            innerFaces = AddNewFace(innerFaces, current, 0);
                        }
                    }
                    if (vertex != current.V1)
                    {
                        if (current.N1 != null)
                        {
                            if (!current.N1.LocalFlag)
                            {
                                if (!current.N1.Marked)
                                {
                                    Tetrahedron<T> neighbour = current.N1;
                                    current.N1 = tetrahedronFactory.Create(current.V0, current.V2, current.V3, oldVertex, current.V1);
                                    current.N1.N3 = current;
                                    neighbour.UpdateLink(current, current.N1);
                                    if (result == null && !current.N1.Infinite) result = current.N1;
                                    if (current.N1.V0 == vertex)
                                    {
                                        current.N1.N0 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N1, 1);
                                        innerFaces = AddNewFace(innerFaces, current.N1, 2);
                                    }
                                    else if (current.N1.V1 == vertex)
                                    {
                                        current.N1.N1 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N1, 0);
                                        innerFaces = AddNewFace(innerFaces, current.N1, 2);
                                    }
                                    else if (current.N1.V2 == vertex)
                                    {
                                        current.N1.N2 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N1, 0);
                                        innerFaces = AddNewFace(innerFaces, current.N1, 1);
                                    }
                                }
                                else if (TryUpdate(current.N1, oldVertex, vertex))
                                {
                                    RemoveFromList(current.N1, ref toProcess);
                                    // Add to local stack
                                    current.N1.Previous = localStack;
                                    localStack = current.N1;
                                    current.N1.LocalFlag = true;
                                }
                                else
                                {
                                    // Disconnect
                                    current.N1.UpdateLink(current, null);
                                    innerFaces = AddNewFace(innerFaces, current, 1);
                                    current.N1 = null;
                                }
                            }
                        }
                        else
                        {
                            innerFaces = AddNewFace(innerFaces, current, 1);
                        }
                    }
                    if (vertex != current.V2)
                    {
                        if (current.N2 != null)
                        {
                            if (!current.N2.LocalFlag)
                            {
                                if (!current.N2.Marked)
                                {
                                    Tetrahedron<T> neighbour = current.N2;
                                    current.N2 = tetrahedronFactory.Create(current.V0, current.V1, current.V3, oldVertex, current.V2);
                                    current.N2.N3 = current;
                                    neighbour.UpdateLink(current, current.N2);
                                    if (result == null && !current.N2.Infinite) result = current.N2;
                                    if (current.N2.V0 == vertex)
                                    {
                                        current.N2.N0 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N2, 1);
                                        innerFaces = AddNewFace(innerFaces, current.N2, 2);
                                    }
                                    else if (current.N2.V1 == vertex)
                                    {
                                        current.N2.N1 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N2, 0);
                                        innerFaces = AddNewFace(innerFaces, current.N2, 2);
                                    }
                                    else if (current.N2.V2 == vertex)
                                    {
                                        current.N2.N2 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N2, 0);
                                        innerFaces = AddNewFace(innerFaces, current.N2, 1);
                                    }
                                }
                                else if (TryUpdate(current.N2, oldVertex, vertex))
                                {
                                    RemoveFromList(current.N2, ref toProcess);
                                    // Add to local stack
                                    current.N2.Previous = localStack;
                                    localStack = current.N2;
                                    current.N2.LocalFlag = true;
                                }
                                else
                                {
                                    // Disconnect
                                    current.N2.UpdateLink(current, null);
                                    innerFaces = AddNewFace(innerFaces, current, 2);
                                    current.N2 = null;
                                }
                            }
                        }
                        else
                        {
                            innerFaces = AddNewFace(innerFaces, current, 2);
                        }
                    }
                    if (vertex != current.V3)
                    {
                        if (current.N3 != null)
                        {
                            if (!current.N3.LocalFlag)
                            {
                                if (!current.N3.Marked)
                                {
                                    Tetrahedron<T> neighbour = current.N3;
                                    current.N3 = tetrahedronFactory.Create(current.V0, current.V1, current.V2, oldVertex, current.V3);
                                    current.N3.N3 = current;
                                    neighbour.UpdateLink(current, current.N3);
                                    if (result == null && !current.N3.Infinite) result = current.N3;
                                    if (current.N3.V0 == vertex)
                                    {
                                        current.N3.N0 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N3, 1);
                                        innerFaces = AddNewFace(innerFaces, current.N3, 2);
                                    }
                                    else if (current.N3.V1 == vertex)
                                    {
                                        current.N3.N1 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N3, 0);
                                        innerFaces = AddNewFace(innerFaces, current.N3, 2);
                                    }
                                    else if (current.N3.V2 == vertex)
                                    {
                                        current.N3.N2 = neighbour;
                                        innerFaces = AddNewFace(innerFaces, current.N3, 0);
                                        innerFaces = AddNewFace(innerFaces, current.N3, 1);
                                    }
                                }
                                else if (TryUpdate(current.N3, oldVertex, vertex))
                                {
                                    RemoveFromList(current.N3, ref toProcess);

                                    // Add to local stack
                                    current.N3.Previous = localStack;
                                    localStack = current.N3;
                                    current.N3.LocalFlag = true;
                                }
                                else
                                {
                                    // Disconnect
                                    current.N3.UpdateLink(current, null);
                                    innerFaces = AddNewFace(innerFaces, current, 3);
                                    current.N3 = null;
                                }
                            }
                        }
                        else
                        {
                            innerFaces = AddNewFace(innerFaces, current, 3);
                        }
                    }
                }
            }

            for (Tetrahedron<T> t = toUnMark; t != null; t = t.Previous)
            {
                t.Marked = false;
                t.LocalFlag = false;
            }

            CreateLinks(innerFaces);

            return result;
        }

        /// <summary>
        /// Removes a tetrahedron from two-directional linked list
        /// </summary>
        private void RemoveFromList(Tetrahedron<T> tetrahedron, ref Tetrahedron<T> list)
        {
            if (tetrahedron.Next != null)
            {
                tetrahedron.Next.Previous = tetrahedron.Previous;
                if (tetrahedron.Previous != null) tetrahedron.Previous.Next = tetrahedron.Next;
            }
            else
            {
                list = tetrahedron.Previous;
                if (list != null) list.Next = null;
            }
        }

        private DisconnectedFace<T> AddNewFace(DisconnectedFace<T> list, Tetrahedron<T> tetrahedron, int face)
        {
            DisconnectedFace<T> newFace = faceFactory.Create(tetrahedron, face);
            newFace.Previous = list;
            return newFace;
        }

        /// <summary>
        /// Attempts to update the tetrahedron to contain the newVertex instead of opposite
        /// </summary>
        private bool TryUpdate(Tetrahedron<T> t, TriangulationVertex<T> opposite, TriangulationVertex<T> newVertex)
        {
            if (t.V0 == opposite)
            {
                if (t.N0 == null || t.N0.Marked) return false;
                t.V0 = newVertex;
            }
            else if (t.V1 == opposite)
            {
                if (t.N1 == null || t.N1.Marked) return false;
                t.V1 = newVertex;
            }
            else if (t.V2 == opposite)
            {
                if (t.N2 == null || t.N2.Marked) return false;
                t.V2 = newVertex;
            }
            else
            {
                if (t.N3 == null || t.N3.Marked) return false;
                t.V3 = newVertex;
            }
            t.VertexUpdated();
            return true;
        }

        /// <summary>
        /// Creates links between faces of new tetrahedra
        /// </summary>
        private void CreateLinks(DisconnectedFace<T> faces)
        {
            while (faces != null)
            {
                DisconnectedFace<T> currentFace = faces;
                faces = faces.Previous;

                int hash = currentFace.Hash & 0x3FF;

                if (FacesBuffer[hash] == null)
                {
                    FacesBuffer[hash] = currentFace;
                    currentFace.Previous = null;
                }
                else if (FacesBuffer[hash].Hash == currentFace.Hash && CanConnect(FacesBuffer[hash], currentFace))
                {
                    DisconnectedFace<T> temp = FacesBuffer[hash];
                    FacesBuffer[hash] = temp.Previous;
                    ConnectFaces(temp, currentFace);
                    faceFactory.Dispose(temp);
                    faceFactory.Dispose(currentFace);
                }
                else
                {
                    DisconnectedFace<T> last = FacesBuffer[hash];
                    for (DisconnectedFace<T> f = last.Previous; f != null; f = f.Previous)
                    {
                        if (currentFace.Hash == f.Hash && CanConnect(f, currentFace))
                        {
                            ConnectFaces(f, currentFace);
                            last.Previous = f.Previous;
                            faceFactory.Dispose(f);
                            faceFactory.Dispose(currentFace);
                            goto end;
                        }
                        else
                        {
                            last = f;
                        }
                    }
                    currentFace.Previous = FacesBuffer[hash];
                    FacesBuffer[hash] = currentFace;
                end: ;
                }
            }
        }

        /// <summary>
        /// Checks whether the two faces have the same vertices
        /// </summary>
        private bool CanConnect(DisconnectedFace<T> face1, DisconnectedFace<T> face2)
        {
            return face1.V0 == face2.V0 && face1.V1 == face2.V1 && face1.V2 == face2.V2;
        }

        /// <summary>
        /// Establishes neighbour relationship between the two tetrahedra
        /// </summary>
        private void ConnectFaces(DisconnectedFace<T> face1, DisconnectedFace<T> face2)
        {
            switch (face1.Face * 4 + face2.Face)
            {
                case 0:
                    face2.Tetrahedron.N0 = face1.Tetrahedron;
                    face1.Tetrahedron.N0 = face2.Tetrahedron;
                    break;
                case 1:
                    face2.Tetrahedron.N1 = face1.Tetrahedron;
                    face1.Tetrahedron.N0 = face2.Tetrahedron;
                    break;
                case 2:
                    face2.Tetrahedron.N2 = face1.Tetrahedron;
                    face1.Tetrahedron.N0 = face2.Tetrahedron;
                    break;
                case 3:
                    face2.Tetrahedron.N3 = face1.Tetrahedron;
                    face1.Tetrahedron.N0 = face2.Tetrahedron;
                    break;
                case 4:
                    face2.Tetrahedron.N0 = face1.Tetrahedron;
                    face1.Tetrahedron.N1 = face2.Tetrahedron;
                    break;
                case 5:
                    face2.Tetrahedron.N1 = face1.Tetrahedron;
                    face1.Tetrahedron.N1 = face2.Tetrahedron;
                    break;
                case 6:
                    face2.Tetrahedron.N2 = face1.Tetrahedron;
                    face1.Tetrahedron.N1 = face2.Tetrahedron;
                    break;
                case 7:
                    face2.Tetrahedron.N3 = face1.Tetrahedron;
                    face1.Tetrahedron.N1 = face2.Tetrahedron;
                    break;
                case 8:
                    face2.Tetrahedron.N0 = face1.Tetrahedron;
                    face1.Tetrahedron.N2 = face2.Tetrahedron;
                    break;
                case 9:
                    face2.Tetrahedron.N1 = face1.Tetrahedron;
                    face1.Tetrahedron.N2 = face2.Tetrahedron;
                    break;
                case 10:
                    face2.Tetrahedron.N2 = face1.Tetrahedron;
                    face1.Tetrahedron.N2 = face2.Tetrahedron;
                    break;
                case 11:
                    face2.Tetrahedron.N3 = face1.Tetrahedron;
                    face1.Tetrahedron.N2 = face2.Tetrahedron;
                    break;
                case 12:
                    face2.Tetrahedron.N0 = face1.Tetrahedron;
                    face1.Tetrahedron.N3 = face2.Tetrahedron;
                    break;
                case 13:
                    face2.Tetrahedron.N1 = face1.Tetrahedron;
                    face1.Tetrahedron.N3 = face2.Tetrahedron;
                    break;
                case 14:
                    face2.Tetrahedron.N2 = face1.Tetrahedron;
                    face1.Tetrahedron.N3 = face2.Tetrahedron;
                    break;
                case 15:
                    face2.Tetrahedron.N3 = face1.Tetrahedron;
                    face1.Tetrahedron.N3 = face2.Tetrahedron;
                    break;
            }
        }

        /// <summary>
        /// Finds the list of tetrahedra that contain the new vertex in their circumspheres
        /// </summary>
        private Tetrahedron<T> Localize(TriangulationVertex<T> vertex, Tetrahedron<T> currentTetrahedron)
        {
            Tetrahedron<T> previous = null;
            double lastDet = currentTetrahedron.SphereDeterminant(vertex);
            int pathLength = 0;
            while (lastDet > 0)
            {
                var temp = currentTetrahedron;
                if (pathLength < 500)
                {
                    currentTetrahedron = FindNextStep(currentTetrahedron, previous, ref lastDet, vertex);
                }
                else
                {
                    // Cycle detected, use a different approach to find the next step
                    currentTetrahedron = FindNextStepDegenerated(currentTetrahedron, previous, ref lastDet, vertex);
                }
                previous = temp;
                pathLength++;
            }

            Tetrahedron<T> neighbours = previous;
            if (neighbours != null)
            {
                neighbours.Previous = null;
                neighbours.Marked = true;
                neighbours.LastDeterminant = neighbours.SphereDeterminant(vertex);
            }

            Tetrahedron<T> toSearch = currentTetrahedron;
            toSearch.Previous = null;
            toSearch.Marked = true;

            Tetrahedron<T> result = null;

            // Find all tetrahedra that contain the new vertex in their circumsphere
            while (toSearch != null)
            {
                currentTetrahedron = toSearch;
                toSearch = currentTetrahedron.Previous;

                currentTetrahedron.LastDeterminant = currentTetrahedron.SphereDeterminant(vertex);
                if (currentTetrahedron.LastDeterminant <= 0)
                {
                    if (result != null) result.Next = currentTetrahedron;
                    currentTetrahedron.Previous = result;
                    result = currentTetrahedron;

                    SearchNext(currentTetrahedron.N0, ref toSearch);
                    SearchNext(currentTetrahedron.N1, ref toSearch);
                    SearchNext(currentTetrahedron.N2, ref toSearch);
                    SearchNext(currentTetrahedron.N3, ref toSearch);
                }
                else
                {
                    currentTetrahedron.Previous = neighbours;
                    neighbours = currentTetrahedron;
                }
            }

            // Unmark all neighbours
            for (Tetrahedron<T> t = neighbours; t != null; t = t.Previous)
                t.Marked = false;

            // Check if the cavity is properly shaped
            for (Tetrahedron<T> t = neighbours; t != null; t = t.Previous)
            {
                if (t.N0.Marked && !t.N0.Infinite)
                {
                    if (t.MinorsQ * t.N0.LastDeterminant - t.N0.MinorsQ * t.LastDeterminant > 1e-16) throw new InvalidOperationException();
                    else if (t.MinorsQ * t.N0.LastDeterminant - t.N0.MinorsQ * t.LastDeterminant > -1e-16 && !PlaneCompareDegenerated(t, 0, vertex)) throw new InvalidOperationException();
                }
                if (t.N1.Marked && !t.N1.Infinite)
                {
                    if (t.MinorsQ * t.N1.LastDeterminant - t.N1.MinorsQ * t.LastDeterminant > 1e-16) throw new InvalidOperationException();
                    else if (t.MinorsQ * t.N1.LastDeterminant - t.N1.MinorsQ * t.LastDeterminant > -1e-16 && !PlaneCompareDegenerated(t, 1, vertex)) throw new InvalidOperationException();
                }
                if (t.N2.Marked && !t.N2.Infinite)
                {
                    if (t.MinorsQ * t.N2.LastDeterminant - t.N2.MinorsQ * t.LastDeterminant > 1e-16) throw new InvalidOperationException();
                    else if (t.MinorsQ * t.N2.LastDeterminant - t.N2.MinorsQ * t.LastDeterminant > -1e-16 && !PlaneCompareDegenerated(t, 2, vertex)) throw new InvalidOperationException();
                }
                if (t.N3.Marked && !t.N3.Infinite)
                {
                    if (t.MinorsQ * t.N3.LastDeterminant - t.N3.MinorsQ * t.LastDeterminant > 1e-16) throw new InvalidOperationException();
                    else if (t.MinorsQ * t.N3.LastDeterminant - t.N3.MinorsQ * t.LastDeterminant > -1e-16 && !PlaneCompareDegenerated(t, 3, vertex)) throw new InvalidOperationException();
                }
            }

            return result;
        }

        /// <summary>
        /// Add new tetrahedron to the stack if it is not marked
        /// </summary>
        private void SearchNext(Tetrahedron<T> neighbour, ref Tetrahedron<T> stack)
        {
            if (!neighbour.Marked)
            {
                neighbour.Marked = true;
                neighbour.Previous = stack;
                stack = neighbour;
            }
        }

        /// <summary>
        /// Find next tetrahedron that is closer to the target point
        /// </summary>
        private Tetrahedron<T> FindNextStep(Tetrahedron<T> current, Tetrahedron<T> previous, ref double currentDeterminant, TriangulationVertex<T> vertex)
        {
            double newDet;

            if (current.N0 != previous)
            {
                newDet = current.N0.SphereDeterminant(vertex);
                if (current.N0.MinorsQ * currentDeterminant - current.MinorsQ * newDet > 0)
                {
                    currentDeterminant = newDet;
                    return current.N0;
                }
            }

            if (current.N1 != previous)
            {
                newDet = current.N1.SphereDeterminant(vertex);
                if (current.N1.MinorsQ * currentDeterminant - current.MinorsQ * newDet > 0)
                {
                    currentDeterminant = newDet;
                    return current.N1;
                }
            }

            if (current.N2 != previous)
            {
                newDet = current.N2.SphereDeterminant(vertex);
                if (current.N2.MinorsQ * currentDeterminant - current.MinorsQ * newDet > 0)
                {
                    currentDeterminant = newDet;
                    return current.N2;
                }
            }

            if (current.N3 != previous)
            {
                newDet = current.N3.SphereDeterminant(vertex);
                if (current.N3.MinorsQ * currentDeterminant - current.MinorsQ * newDet > 0)
                {
                    currentDeterminant = newDet;
                    return current.N3;
                }
            }

            // Degenerate configuration found, use an exact evaluation method
            return FindNextStepDegenerated(current, previous, ref currentDeterminant, vertex);
        }

        /// <summary>
        /// Find next tetrahedron that is closer to the target point using exact plane-point tests
        /// </summary>
        private Tetrahedron<T> FindNextStepDegenerated(Tetrahedron<T> current, Tetrahedron<T> previous, ref double currentDeterminant, TriangulationVertex<T> vertex)
        {
            double m00 = current.V1.X - current.V0.X;
            double m01 = current.V1.Y - current.V0.Y;
            double m02 = current.V1.Z - current.V0.Z;
            double m03 = current.V0.X * current.V1.Y - current.V1.X * current.V0.Y;
            double m04 = current.V0.X * current.V1.Z - current.V1.X * current.V0.Z;
            double m05 = current.V0.Y * current.V1.Z - current.V1.Y * current.V0.Z;
            double m10 = current.V3.X - current.V2.X;
            double m11 = current.V3.Y - current.V2.Y;
            double m12 = current.V3.Z - current.V2.Z;
            double m13 = current.V2.X * current.V3.Y - current.V3.X * current.V2.Y;
            double m14 = current.V2.X * current.V3.Z - current.V3.X * current.V2.Z;
            double m15 = current.V2.Y * current.V3.Z - current.V3.Y * current.V2.Z;
            double q = m00 * m15 - m01 * m14 + m02 * m13 + m03 * m12 - m04 * m11 + m05 * m10;

            if (current.N0 != previous)
            {
                double det = (current.V1.X - vertex.X) * m15 - (current.V1.Y - vertex.Y) * m14 + (current.V1.Z - vertex.Z) * m13 + (vertex.X * current.V1.Y - current.V1.X * vertex.Y) * m12 - (vertex.X * current.V1.Z - current.V1.X * vertex.Z) * m11 + (vertex.Y * current.V1.Z - current.V1.Y * vertex.Z) * m10;
                if (det * q < 0)
                {
                    currentDeterminant = current.N0.SphereDeterminant(vertex);
                    return current.N0;
                }
            }
            if (current.N1 != previous)
            {
                double det = (vertex.X - current.V0.X) * m15 - (vertex.Y - current.V0.Y) * m14 + (vertex.Z - current.V0.Z) * m13 + (current.V0.X * vertex.Y - vertex.X * current.V0.Y) * m12 - (current.V0.X * vertex.Z - vertex.X * current.V0.Z) * m11 + (current.V0.Y * vertex.Z - vertex.Y * current.V0.Z) * m10;
                if (det * q < 0)
                {
                    currentDeterminant = current.N1.SphereDeterminant(vertex);
                    return current.N1;
                }
            }
            if (current.N2 != previous)
            {
                double det = m00 * (vertex.Y * current.V3.Z - current.V3.Y * vertex.Z) - m01 * (vertex.X * current.V3.Z - current.V3.X * vertex.Z) + m02 * (vertex.X * current.V3.Y - current.V3.X * vertex.Y) + m03 * (current.V3.Z - vertex.Z) - m04 * (current.V3.Y - vertex.Y) + m05 * (current.V3.X - vertex.X);
                if (det * q < 0)
                {
                    currentDeterminant = current.N2.SphereDeterminant(vertex);
                    return current.N2;
                }
            }
            if (current.N3 != previous)
            {
                double det = m00 * (current.V2.Y * vertex.Z - vertex.Y * current.V2.Z) - m01 * (current.V2.X * vertex.Z - vertex.X * current.V2.Z) + m02 * (current.V2.X * vertex.Y - vertex.X * current.V2.Y) + m03 * (vertex.Z - current.V2.Z) - m04 * (vertex.Y - current.V2.Y) + m05 * (vertex.X - current.V2.X);
                if (det * q < 0)
                {
                    currentDeterminant = current.N3.SphereDeterminant(vertex);
                    return current.N3;
                }
            }

            // Triangulation is in a degenerate shape
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Computes exact plane-vertex position test
        /// </summary>
        private bool PlaneCompareDegenerated(Tetrahedron<T> current, int face, TriangulationVertex<T> vertex)
        {
            double m00 = current.V1.X - current.V0.X;
            double m01 = current.V1.Y - current.V0.Y;
            double m02 = current.V1.Z - current.V0.Z;
            double m03 = current.V0.X * current.V1.Y - current.V1.X * current.V0.Y;
            double m04 = current.V0.X * current.V1.Z - current.V1.X * current.V0.Z;
            double m05 = current.V0.Y * current.V1.Z - current.V1.Y * current.V0.Z;
            double m10 = current.V3.X - current.V2.X;
            double m11 = current.V3.Y - current.V2.Y;
            double m12 = current.V3.Z - current.V2.Z;
            double m13 = current.V2.X * current.V3.Y - current.V3.X * current.V2.Y;
            double m14 = current.V2.X * current.V3.Z - current.V3.X * current.V2.Z;
            double m15 = current.V2.Y * current.V3.Z - current.V3.Y * current.V2.Z;
            double q = m00 * m15 - m01 * m14 + m02 * m13 + m03 * m12 - m04 * m11 + m05 * m10;

            if (face == 0)
            {
                double det = (current.V1.X - vertex.X) * m15 - (current.V1.Y - vertex.Y) * m14 + (current.V1.Z - vertex.Z) * m13 + (vertex.X * current.V1.Y - current.V1.X * vertex.Y) * m12 - (vertex.X * current.V1.Z - current.V1.X * vertex.Z) * m11 + (vertex.Y * current.V1.Z - current.V1.Y * vertex.Z) * m10;
                return (det * q < 0);
            }
            else if (face == 1)
            {
                double det = (vertex.X - current.V0.X) * m15 - (vertex.Y - current.V0.Y) * m14 + (vertex.Z - current.V0.Z) * m13 + (current.V0.X * vertex.Y - vertex.X * current.V0.Y) * m12 - (current.V0.X * vertex.Z - vertex.X * current.V0.Z) * m11 + (current.V0.Y * vertex.Z - vertex.Y * current.V0.Z) * m10;
                return (det * q < 0);
            }
            else if (face == 2)
            {
                double det = m00 * (vertex.Y * current.V3.Z - current.V3.Y * vertex.Z) - m01 * (vertex.X * current.V3.Z - current.V3.X * vertex.Z) + m02 * (vertex.X * current.V3.Y - current.V3.X * vertex.Y) + m03 * (current.V3.Z - vertex.Z) - m04 * (current.V3.Y - vertex.Y) + m05 * (current.V3.X - vertex.X);
                return (det * q < 0);
            }
            else
            {
                double det = m00 * (current.V2.Y * vertex.Z - vertex.Y * current.V2.Z) - m01 * (current.V2.X * vertex.Z - vertex.X * current.V2.Z) + m02 * (current.V2.X * vertex.Y - vertex.X * current.V2.Y) + m03 * (vertex.Z - current.V2.Z) - m04 * (vertex.Y - current.V2.Y) + m05 * (vertex.X - current.V2.X);
                return (det * q < 0);
            }
        }
    }
}
