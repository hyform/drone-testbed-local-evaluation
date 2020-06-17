using System;

namespace DesignerObjects
{

    /// <summary>
    /// 
    /// Connector object that stores the starting and ending joints, along
    /// with a boolean if a component is included at the end of the connection.
    /// A component is not included at the end if there is a cycle in the assembly
    /// 
    /// </summary>
    class ConnectorInfo
    {

        /// <summary>
        /// starting node index
        /// </summary>
        public int x1 { get; set; }

        /// <summary>
        /// ending node index
        /// </summary>
        public int x2 { get; set; }

        /// <summary>
        /// added a component at the end of the connection
        /// </summary>
        public bool addedComponent { get; set; }

        /// <summary>
        ///  
        /// main constructor
        /// 
        /// </summary>
        /// <param name="x1">starting node index</param>
        /// <param name="x2">ending node index</param>
        /// <param name="addedComponent">added a component to the end of the connection</param>
        public ConnectorInfo(int x1, int x2, bool addedComponent)
        {
            this.x1 = x1;
            this.x2 = x2;
            this.addedComponent = addedComponent;
        }

        /// <summary>
        /// string representation of the connector
        /// </summary>
        /// <returns></returns>
        public String grammar()
        {
            return "^" + JointInfo.nodeIdChars[x1] + "" + JointInfo.nodeIdChars[x2];
        }

    }

}
