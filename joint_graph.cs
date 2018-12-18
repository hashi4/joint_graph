using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

using PEPlugin;
using PEPlugin.Pmx;
using PEPlugin.Pmd;
using PEPlugin.SDX;
using PEPlugin.Form;
using PEPlugin.View;

namespace JointGraph
{
    // typedefs
    using NodeSet = HashSet<Node>;
    using EdgeDict = Dictionary<Node, HashSet<Node>>;

    internal class Node {
        internal string id;
        internal object pmxElement;

        internal Node(string id, Object element) {
            this.id = id;
            this.pmxElement = element;
        }

        public override bool Equals(object other) {
            if (null != other && other.GetType() == this.GetType()) {
                return this.id == ((Node)other).id;
            } else {
                return false;
            }
        }

        public override int GetHashCode() {
            return id.GetHashCode();
        }
    }

    internal class JointGraph {
        internal EdgeDict directedEdges;
        internal EdgeDict unDirectedEdges;
        internal IEnumerable<Node> nodes {
            get { return directedEdges.Keys; }
        }

        internal JointGraph() {
            this.directedEdges= new EdgeDict();
            this.unDirectedEdges = new EdgeDict();
        }

        internal void addNode(Node node) {
            EdgeDict[] el = new EdgeDict[] {
                this.directedEdges,
                this.unDirectedEdges
            };

            foreach (EdgeDict e in el) {
                if (!e.ContainsKey(node)) {
                    e[node] = new NodeSet();
                }
            }
            return;
        }
            
        internal void addEdge(Node fromNode, Node toNode) {
            this.addNode(fromNode);
            this.addNode(toNode);
            this.directedEdges[fromNode].Add(toNode);
            this.unDirectedEdges[fromNode].Add(toNode);
            this.unDirectedEdges[toNode].Add(fromNode);
        }
    
        private NodeSet traceGraph(Node fromNode, EdgeDict edgeDict,
                    NodeSet visited = null) {
            if (null == visited) {
                visited = new NodeSet();
            }
            if (!visited.Contains(fromNode)) {
                visited.Add(fromNode);
                foreach (Node linkedNode in edgeDict[fromNode]) {
                    visited = traceGraph(linkedNode, edgeDict, visited);
                }
            }
            return visited;
        }

        internal NodeSet getDescendants(Node fromNode) {
            return this.traceGraph(fromNode, this.directedEdges);
        }

        internal NodeSet getConnectedNodes(Node fromNode) {
            return this.traceGraph(fromNode, this.unDirectedEdges);
        }
    }

    //////////////////////////

    public class CSScriptClass: PEPluginClass
    {
        // object -> index
        private static Dictionary<IPXBone, int> boneIndexDict;
        private static Dictionary<IPXBody, int> bodyIndexDict;
        private static Dictionary<IPXJoint, int> jointIndexDict;


        public CSScriptClass(): base() {
            m_option = new PEPluginOption(false , true ,
                "ジョイント接続グラフをGraphviz形式で出力");
        }

        public override void Run(IPERunArgs args) {
            try {
                IPEPluginHost host = args.Host;
                IPEConnector connect = host.Connector;
                IPEViewConnector view = host.Connector.View;
                IPEFormConnector form = host.Connector.Form;
                IPXPmx pmx = connect.Pmx.GetCurrentState();
                plugin_main(pmx, view, form);
                connect.View.PMDView.UpdateView();
            } catch (Exception ex) {
                MessageBox.Show(
                    ex.Message , "エラー" , MessageBoxButtons.OK ,
                    MessageBoxIcon.Exclamation);
            }
        }

        private static string selectFile() {
            SaveFileDialog diag = new SaveFileDialog();
            diag.Filter = "Graphviz形式(*.dot)|*.dot";
            diag.FilterIndex = 2;
            diag.RestoreDirectory = true;
            diag.OverwritePrompt = true;
            if (diag.ShowDialog() == DialogResult.OK) {
                return diag.FileName;
            } else {
                return null;
            }
        }

        // pmx element -> index
        private static void makeIndexDict(IPXPmx pmx) {
                boneIndexDict = new Dictionary<IPXBone, int>();
                bodyIndexDict = new Dictionary<IPXBody, int>();
                jointIndexDict = new Dictionary<IPXJoint, int>();

            for (int i = 0; i < pmx.Bone.Count(); i++) {
                boneIndexDict[pmx.Bone[i]] = i;
            }
            for (int i = 0; i < pmx.Body.Count(); i++) {
                bodyIndexDict[pmx.Body[i]] = i;
            }
            for (int i = 0; i < pmx.Joint.Count(); i++) {
                jointIndexDict[pmx.Joint[i]] = i;
            }
            return;
        }

        private static Node makeJointNode(IPXJoint joint) {
            int index = jointIndexDict[joint];
            string jointID =  "JOINT_" + index.ToString();
            return new Node(jointID, joint);
        }

        private static Node makeBodyNode(IPXBody body) {
            int index = bodyIndexDict[body];
            string bodyID = "BODY_" + index.ToString();
            return new Node(bodyID, body);
        }

        private static JointGraph makeJointGraph(IPXPmx pmx) {
            JointGraph jointGraph = new JointGraph();
            foreach (IPXJoint joint in pmx.Joint) {
                Node jointNode = makeJointNode(joint);
                if (null != joint.BodyA) {
                    Node bodyANode = makeBodyNode(joint.BodyA);
                    jointGraph.addEdge(bodyANode, jointNode);
                }
                if (null != joint.BodyB) {
                    Node bodyBNode = makeBodyNode(joint.BodyB);
                    jointGraph.addEdge(jointNode, bodyBNode);
                }
            }
            // register isolated rigid bodies
            foreach (IPXBody body in pmx.Body) {
                jointGraph.addNode(makeBodyNode(body));
            }
            return jointGraph;
        }
        
        private static StreamWriter makeWriter() {
            string dotFileName = selectFile();
            if (null == dotFileName) {
                throw new System.Exception("ファイルを選択してください");
            }
            return new StreamWriter(
                new FileStream(dotFileName, FileMode.Create),
                new UTF8Encoding(false)); // without BOM
        }

        private static void writeHeader(StreamWriter writer) {
            writer.WriteLine("digraph Joint_Body_Graph {");
            writer.WriteLine("graph [charset = \"UTF-8\"];");
            writer.WriteLine(
                "node[fontname=\"meiryo\", fillcolor=\"lightgray\"];");
        }

        private static void writeFooter(StreamWriter writer) {
            writer.WriteLine("}");
        }

        private static void writeJointNode(
                Node node, StreamWriter writer, bool fill = false) {

            IPXJoint joint = (IPXJoint)node.pmxElement;
            string shape = "box";
            string style = "solid";
            if (fill) style += ",filled";
            int index = jointIndexDict[joint];
            writer.WriteLine(String.Format(
                "{0} [shape = {1}, label = \"{2:D}: {3}\", style = \"{4}\"];",
                node.id, shape, index, joint.Name, style));
        }

        private static void writeBodyNode(
                Node node, StreamWriter writer, bool fill = false) {

            IPXBody body = (IPXBody)node.pmxElement;
            string shape = "ellipse";
            Dictionary<BodyMode, string> styleDict =
                    new Dictionary<BodyMode, string>() {
                {BodyMode.Static, "solid"},
                {BodyMode.Dynamic, "dashed"},
                {BodyMode.DynamicWithBone, "dotted"}
            };
            string style = styleDict[body.Mode];
            if (fill) style += ",filled";
            int index = bodyIndexDict[body];
            if (null == body.Bone) {
                writer.WriteLine(String.Format(
                    "{0} [shape = {1}, label = \"{2:D}: {3}\", style = \"{4}\"];",
                    node.id, shape, index, body.Name, style));
            } else {
                int boneIndex = boneIndexDict[body.Bone];
                writer.WriteLine(String.Format(
                    "{0} [shape = {1}, label = \"{2:D}: {3}\\n[{5:D}: {6}]\", style = \"{4}\"];",
                    node.id, shape, index, body.Name, style,
                    boneIndex, body.Bone.Name));
            }
        }

        private static void writeNode(
                Node node, StreamWriter writer, bool fill=false) {
            if (node.id.StartsWith("JOINT_")) {
                writeJointNode(node, writer, fill);
            } else if (node.id.StartsWith("BODY_")) {
                writeBodyNode(node, writer, fill);
            } else {
                throw new System.Exception("bug!");
            }
        }

        private static void writeEdge(Node fromNode, NodeSet toNodes,
                StreamWriter writer) {
            foreach (Node toNode in toNodes) {
                writer.WriteLine(String.Format(
                    "{0} -> {1};", fromNode.id, toNode.id));
            }
        }

        private static void toDot(
                JointGraph g, StreamWriter writer,
                NodeSet nodes = null, NodeSet selected=null) {

            IEnumerable<Node> printNodes;
            if (null == nodes) {
                printNodes = g.nodes;
            } else {
                printNodes = nodes;
            }
            writeHeader(writer);
            foreach (Node node in printNodes) {
                if (null != selected && selected.Contains(node)) {
                    writeNode(node, writer, true);  // fill color
                } else {
                    writeNode(node, writer);
                }
            }
            EdgeDict edges = g.directedEdges;
            foreach (Node node in printNodes) {
                writeEdge(node, edges[node], writer);
            }
            writeFooter(writer);
        }

        private static void plugin_main(
                IPXPmx pmx, IPEViewConnector view, IPEFormConnector form) {

            StreamWriter writer = makeWriter();
            using (writer) {
                int[] selectedBodies = view.PmxView.GetSelectedBodyIndices();
                int[] selectedJoints = view.PmxView.GetSelectedJointIndices();

                makeIndexDict(pmx);
                JointGraph g = makeJointGraph(pmx);

                NodeSet subNodes = new NodeSet();
                NodeSet selectedNodes = new NodeSet();
                // pmxvewの選択情報は選択解除しても残ってしまうので
                // form の情報で判断する
                if (form.SelectedJointIndex >= 0) {
                    foreach (int i in selectedJoints) {
                        Node jointNode = makeJointNode(pmx.Joint[i]);
                        selectedNodes.Add(jointNode);
                        subNodes.UnionWith(
                            g.getConnectedNodes(jointNode));
                    }
                }
                if (form.SelectedBodyIndex >= 0) {
                    foreach (int i in selectedBodies) {
                        Node bodyNode = makeBodyNode(pmx.Body[i]);
                        selectedNodes.Add(bodyNode);
                        subNodes.UnionWith(
                            g.getConnectedNodes(bodyNode));
                    }
                }
                if (selectedNodes.Count > 0) {
                    toDot(g, writer, subNodes, selectedNodes);
                } else {
                    toDot(g, writer);
                }
                return;
            }
        }
    }
}
