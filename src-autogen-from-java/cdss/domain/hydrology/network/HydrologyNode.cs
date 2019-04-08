using System;
using System.Collections.Generic;

// ----------------------------------------------------------------------------
// HydrologyNode - a representation of a node on a stream network
// ----------------------------------------------------------------------------

/* NoticeStart

CDSS Java Library
CDSS Java Library is a part of Colorado's Decision Support Systems (CDSS)
Copyright (C) 1994-2019 Colorado Department of Natural Resources

CDSS Java Library is free software:  you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    CDSS Java Library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with CDSS Java Library.  If not, see <https://www.gnu.org/licenses/>.

NoticeEnd */

// Notes:	(1)	This code is meant to support (at least) three
//			CRDSS applications:
//
//			  makenet - used by planning model to generate network
//			  StateMod GUI - to allow interactive network edits
//			  Admin tool - to allow call analysis, water balance,
//					etc.
//
//			The data members for each capability are listed
//			separately, where appropriate.
//		(2)	It may be necessary or desirable to make a base class
//			and derive different node types (e.g., StateModNode,
//			WISNode) from that, but for now keep in one class.
// ----------------------------------------------------------------------------
// History:
// 
// 27 Oct 1997	Steven A. Malers,	Initial version - from makenet.
//		Riverside Technology,
//		inc.
// 28 Feb 1998	SAM, RTi		Final cleanup for makenet.
// 19 Feb 1999	Daniel Weiler, RTi	Added Wells.
// 31 Mar 1999	SAM, RTi		Code sweep.
// 07 Apr 1999	SAM, RTi		Add __isDryRiver flag to support WIS
//					(and potentially makene in Rio Grande).
// 27 Jul 1999	SAM, RTi		Update to have XCONFL node type.
// 18 Jul 1999	SAM, RTi		Add isValidType() for use by watright
//					and other applications.
// 15 Nov 1999	CEN, RTi		Added __labelAngle member and assoc.
//					functions.
// 06 Dec 1999	SAM, RTi		Add NODE_TYPE_DIV_AND_WELL to support
//					makenet and add to check method.
//					isValidType() is not used.  Instead, use
//					lookupType() and check for negative
//					return value.
// ----------------------------------------------------------------------------
// 2003-10-08	J. Thomas Sapienza, RTi	Updated to HydroBaseDMI.
// 2003-12-15	JTS, RTi		* Javadoc cleanup.
//					* Now extends DMIDataObject.
//					* Added visible member variable.
//					* Added initial hooks for the node to
//					  draw itself on a GRDrawingArea.
// 2004-04	JTS, RTi		Made numerous additions to class, most
//					in response to new needs dictated by
//					the network diagram drawing and plotting
//					code.
// 2004-05-18	JTS, RTi		Added setPosition().
// 2004-07-09	JTS, RTi		Added isImport.
//		SAM, RTi		Remove some new String() notation - it
//					is not needed.
//					Add some javadoc explaining the node
//					type abbreviations versus full type
//					strings.
// 2004-07-21	JTS, RTi		Added getVerboseWISType() for use with
//					WIS networks.
// 2004-12-20	JTS, RTi		Changed how label positions are numbered
//					so that original Makenet networks 
//					display properly.
// 2005-04-28	JTS, RTi		Added all data members to finalize().
// 2005-12-21	JTS, RTi		&, < and > in any node IDs are now
//					escaped.
// 2007-02-26	SAM, RTi		Clean up code based on Eclipse feedback.
// ----------------------------------------------------------------------------
// EndHeader

namespace cdss.domain.hydrology.network
{

	using DMIDataObject = RTi.DMI.DMIDataObject;
	using DMIUtil = RTi.DMI.DMIUtil;

	using GRColor = RTi.GR.GRColor;
	using GRDrawingAreaUtil = RTi.GR.GRDrawingAreaUtil;
	using GRJComponentDrawingArea = RTi.GR.GRJComponentDrawingArea;
	using GRLimits = RTi.GR.GRLimits;
	using GRSymbol = RTi.GR.GRSymbol;
	using GRText = RTi.GR.GRText;
	using GRUnits = RTi.GR.GRUnits;

	using Message = RTi.Util.Message.Message;

	using StringUtil = RTi.Util.String.StringUtil;


	/// <summary>
	/// This class stores basic node information for use with HydroBase_NodeNetwork.
	/// </summary>
	public class HydrologyNode : DMIDataObject
	{

	/// <summary>
	/// Class name.
	/// </summary>
	private readonly string __CLASS = "HydrologyNode";

	/// <summary>
	/// Blank node for spacing nodes on the Makenet plot.
	/// </summary>
	public const int NODE_TYPE_BLANK = 0;

	/// <summary>
	/// Diversion node.
	/// </summary>
	public const int NODE_TYPE_DIV = 1;

	/// <summary>
	/// Streamflow node (stream gage).
	/// </summary>
	public const int NODE_TYPE_FLOW = 2;

	/// <summary>
	/// Confluence node.
	/// </summary>
	public const int NODE_TYPE_CONFLUENCE = 3;

	/// <summary>
	/// Instream flow node.
	/// </summary>
	public const int NODE_TYPE_ISF = 4;

	/// <summary>
	/// Reservoir node.
	/// </summary>
	public const int NODE_TYPE_RES = 5;

	/// <summary>
	/// Import node.
	/// </summary>
	public const int NODE_TYPE_IMPORT = 6;

	/// <summary>
	/// TODO SAM 2008-12-10 Remove when natural flow is fully enabled and "other" node type use is confirmed
	/// in user files.
	/// Baseflow node (stream estimate).  Note that this should not typically be used but is retained
	/// for historical reasons.  DO NOT CONVERT THIS TO NATURAL_FLOW as there is no natural flow node type.
	/// </summary>
	public const int NODE_TYPE_BASEFLOW = 7;

	/// <summary>
	/// Node at the bottom of a network.
	/// </summary>
	public const int NODE_TYPE_END = 8;

	/// <summary>
	/// Other node type (unclassified).
	/// </summary>
	public const int NODE_TYPE_OTHER = 9;

	/// <summary>
	/// Unknown node type for initialization and StateMod .rin files that do not have the node type.
	/// </summary>
	public const int NODE_TYPE_UNKNOWN = 10;

	/// <summary>
	/// Used for top of stream reaches.
	/// </summary>
	public const int NODE_TYPE_STREAM = 11;

	/// <summary>
	/// Used for label points.
	/// </summary>
	public const int NODE_TYPE_LABEL = 12;

	/// <summary>
	/// Used for formula cells.
	/// </summary>
	public const int NODE_TYPE_FORMULA = 13;

	/// <summary>
	/// Well node (no connection to surface water ditch).
	/// </summary>
	public const int NODE_TYPE_WELL = 14;

	/// <summary>
	/// Confluence node for downstream end of off-channel stream.
	/// </summary>
	public const int NODE_TYPE_XCONFLUENCE = 15;

	/// <summary>
	/// Well node associated with surface water ditch.
	/// </summary>
	public const int NODE_TYPE_DIV_AND_WELL = 16;

	/// <summary>
	/// Node only used in the network drawing code.  This is a node that functions as
	/// a user-defined label on the screen.
	/// </summary>
	public const int NODE_TYPE_LABEL_NODE = 17;

	/// <summary>
	/// Plan station, used with StateMod only.
	/// </summary>
	public const int NODE_TYPE_PLAN = 18;

	/// <summary>
	/// Indicate how upstream nodes are constructed.  The default is to add tributaries
	/// first (like makenet) but CWRAT adds the main stem first.
	/// </summary>
	public const int TRIBS_ADDED_FIRST = 1, TRIBS_ADDED_LAST = 2;

	/// <summary>
	/// To determine the kind of names to return.
	/// </summary>
	public const int FULL = 0, ABBREVIATION = 1;

	/// <summary>
	/// Whether to draw the node text (the label like station ID) or not.
	/// </summary>
	private static bool __drawText = true;

	/// <summary>
	/// Whether the node is being drawn in the WIS network display.
	/// </summary>
	/*
	private boolean __inWis = true;
	*/
	//FIXME SAM 2008-03-15 Need to remove WIS from this general class

	/// <summary>
	/// Whether the node is a natural flow location.
	/// </summary>
	private bool __isNaturalFlow;

	/// <summary>
	/// Whether the node is an import node or not. 
	/// </summary>
	private bool __isImport;

	/// <summary>
	/// Whether the river is dry at the node.  WIS-specific.
	/// </summary>
	private bool __isDryRiver;

	/// <summary>
	/// Area as float.  Makenet-specific.
	/// </summary>
	private double __area;

	/// <summary>
	/// Angle to print label at.  0 == East.  Makenet-specific.
	/// </summary>
	private double __labelAngle;

	/// <summary>
	/// Precipitation as float.  Makenet-specific.
	/// </summary>
	private double __precip;

	/// <summary>
	/// The proration factor to distribute the gain.  Makenet-specific.
	/// </summary>
	private double __prorationFactor;

	/// <summary>
	/// Stream mile for structure (overall from border).  WIS-specific.
	/// </summary>
	private double __streamMile;

	/// <summary>
	/// Area * precip as float.  Makenet-specific.
	/// </summary>
	private double __water;

	/// <summary>
	/// Plotting X coordinate for node, in virtual page space.
	/// </summary>
	private double __x;
	/// <summary>
	/// Plotting Y coordinate for node, in virtual page space.
	/// </summary>
	private double __y;

	/// <summary>
	/// Pointer to the node directly downstream of this node.
	/// </summary>
	private HydrologyNode __downstream;

	/// <summary>
	/// WIS format row associated with this node.  WIS-specific.
	/// </summary>
	//private HydroBase_WISFormat __wisFormat;
	// FIXME SAM 2008-03-15 Need to remove WIS from this general class

	/// <summary>
	/// The size of the icon in drawing units (points for printing, pixels for screen).
	/// This is a space that will be filled by the specific symbol that is chosen for the node.
	/// </summary>
	private int __iconDiameter = 20;

	/// <summary>
	/// The extra space around the node icon in drawing units for displaying the extra shape decorator
	/// showing that it is a natural flow node (big circle) or import (big square).  For example, if
	/// the icon diameter is 20, the extra diameter is generally 1/3 of this value (to be added to the icon diameter.
	/// </summary>
	private int __decoratorDiameter = 6;

	/// <summary>
	/// Computational order of nodes, with 1 being most upstream.  This generally has 
	/// to be set after the entire network has been populated. 
	/// </summary>
	private int __computationalOrder = -1;

	/// <summary>
	/// Label direction (for plotting network).  Makenet-specific.
	/// </summary>
	private int __labelDir;

	/// <summary>
	/// The node number in the reach (starting at one).
	/// </summary>
	private int __nodeInReachNum;

	/// <summary>
	/// The reach number counting the total number of streams in the system.  Therefore
	/// the first reach in the system is 1, the next reach added is 2, etc.
	/// </summary>
	private int __reachCounter;

	/// <summary>
	/// The level of the river with 1 being the main stem.
	/// </summary>
	private int __reachLevel;

	/// <summary>
	/// Serial integer used to keep a running count of the nodes in the network.
	/// </summary>
	private int __serial;

	/// <summary>
	/// The number of tribs to the parent stream (starting at one).  In other words,
	/// if the downstream node has multiple upstream nodes, this is the counter for
	/// those nodes.  That allows a search coming from upstream to know which reach
	/// it is coming from.  Mainly important on nodes above a confluence.
	/// </summary>
	private int __tributaryNum;

	/// <summary>
	/// Node type.
	/// </summary>
	private int __type;

	/// <summary>
	/// How upstream nodes are constructed.  See TRIBS_*.
	/// </summary>
	private int __upstreamOrder;

	/// <summary>
	/// Link data for confluences.
	/// </summary>
	private long __link;

	/// <summary>
	/// Equivalent to wdwater_num or stream_num (when that gets implemented).  
	/// WIS-specific.
	/// </summary>
	private long __streamNum;

	/// <summary>
	/// An object of any kind that can be associated with a node.
	/// </summary>
	private object __associatedObject;

	/// <summary>
	/// Area as string.  Makenet-specific.
	/// </summary>
	private string __areaString;

	/// <summary>
	/// Node description.
	/// </summary>
	private string __desc;

	/// <summary>
	/// Common ID.
	/// </summary>
	private string __commonID;

	/// <summary>
	/// ID from the net file.  Makenet-specific.
	/// </summary>
	private string __netID;

	/// <summary>
	/// Precipitation as string.  Makenet-specific.
	/// </summary>
	private string __precipString;

	/// <summary>
	/// River node to use in output.  Makenet-specific.
	/// </summary>
	private string __riverNodeID;

	/// <summary>
	/// User description as set during the processMakenet process.  Makenet-specific.
	/// </summary>
	private string __userDesc;

	/// <summary>
	/// Area * precip as String.  Makenet-specific.
	/// </summary>
	private string __waterString;

	/// <summary>
	/// List of nodes directly upstream of this node.
	/// </summary>
	private IList<HydrologyNode> __upstream = new List<HydrologyNode>();

	//--------------------------------------------------------------------------
	// Data members used solely with the network drawing code.
	//--------------------------------------------------------------------------

	/// <summary>
	/// Whether this nodes drawing bounds have been calculated or not.
	/// </summary>
	private bool __boundsCalculated = false;

	/// <summary>
	/// Whether this node was selected with a mouse drag.
	/// </summary>
	private bool __isSelected = false;

	/// <summary>
	/// Whether this node was already stored in the database or was generated new
	/// for the current network drawer.
	/// </summary>
	private bool __readFromDB = false;

	/// <summary>
	/// Whether this node is visible or not.
	/// </summary>
	private bool __visible = true;

	/// <summary>
	/// Whether calls should be shown.
	/// </summary>
	private bool __showCalls = false;

	/// <summary>
	/// Whether the delivery flow data should be shown.
	/// </summary>
	private bool __showDeliveryFlow = false;

	/// <summary>
	/// Whether the natural flow data should be shown.
	/// </summary>
	private bool __showNaturalFlow = false;

	/// <summary>
	/// Whether the point flow data should be shown.
	/// </summary>
	private bool __showPointFlow = false;

	/// <summary>
	/// Whether the rights should be shown.
	/// </summary>
	private bool __showRights = false;

	/// <summary>
	/// The delivery flow value.
	/// </summary>
	private double __deliveryFlow = DMIUtil.MISSING_DOUBLE;

	/// <summary>
	/// The original UTM X and Y values read for this node's structure from the database.
	/// </summary>
	private double __dbX = DMIUtil.MISSING_DOUBLE, __dbY = DMIUtil.MISSING_DOUBLE;

	/// <summary>
	/// The height of the area drawn by this node.
	/// </summary>
	private double __height;

	/// <summary>
	/// The natural flow value.
	/// </summary>
	private double __naturalFlow = DMIUtil.MISSING_DOUBLE;

	/// <summary>
	/// The point flow value.
	/// </summary>
	private double __pointFlow = DMIUtil.MISSING_DOUBLE;

	/// <summary>
	/// The width of the area drawn by this node.
	/// </summary>
	private double __width;

	/// <summary>
	/// The symbol drawn along with this node.  Depends on the type of node it is. 
	/// Can be null, in which case no symbol will be drawn.
	/// </summary>
	private GRSymbol __symbol = null;

	/// <summary>
	/// A secondary symbol drawn in conjunction with the primary symbol.  If null, then it won't be drawn.
	/// For example, the end node has a primary symbol of a circle and a secondary symbol of an iX.
	/// Reservoirs also have a primary symbol of a circle, with a secondary symbol of an inner triangle.
	/// </summary>
	private GRSymbol __secondarySymbol = null;

	/// <summary>
	/// The wis num of the wis that this node is associated with.
	/// </summary>
	private int __wisNum = DMIUtil.MISSING_INT;

	/// <summary>
	/// Call information.
	/// </summary>
	private string __call = DMIUtil.MISSING_STRING;

	/// <summary>
	/// The unique identifier for this node.  Similar to the common ID.
	/// </summary>
	private string __identifier = null;

	/// <summary>
	/// The label drawn along with this node.
	/// </summary>
	private string __label = null;

	/// <summary>
	/// The verbose kind of node this is, as a string.
	/// </summary>
	private string __nodeType = null;

	/// <summary>
	/// Right information.
	/// </summary>
	private string __right = DMIUtil.MISSING_STRING;

	/// <summary>
	/// The text to be drawn inside the node symbol.
	/// </summary>
	private string __symText = null;

	/// <summary>
	/// The id of the node immediately downstream of this node.
	/// </summary>
	private string __downstreamNodeID = null;

	/// <summary>
	/// The ids of all the nodes immediately upstream of this node.
	/// </summary>
	private IList<string> __upstreamNodeIDs = null;

	/// <summary>
	/// Constructor.  
	/// Constructs node and initializes to reasonable values(primarily empty strings and zero or -1 values.
	/// </summary>
	public HydrologyNode()
	{
		initialize();
	}

	/// <summary>
	/// Add a node downstream from this node. </summary>
	/// <param name="downstream_node"> Downstream node to add. </param>
	/// <returns> true if successful, false if not. </returns>
	public virtual bool addDownstreamNode(HydrologyNode downstream_node)
	{
		string routine = __CLASS + ".addDownstreamNode";
		int dl = 50;

		try
		{

		if (Message.isDebugOn)
		{
			Message.printDebug(dl, routine, "Adding \"" + downstream_node.getCommonID() + "\" downstream of \"" + getCommonID() + "\"");
		}

		HydrologyNode oldDownstreamNode = __downstream;

		if (__downstream != null)
		{
			// There is a downstream node and we need to reconnect it...

			// For the original downstream node, reset its upstream reference to the new node.
			// Use the common identifier to find the element to reset...
			int pos = __downstream.getUpstreamNodePosition(getCommonID());
			if (pos >= 0)
			{
				IList<HydrologyNode> downstreamUpstream = __downstream.getUpstreamNodes();
				if (downstreamUpstream != null)
				{
					downstreamUpstream[pos] = downstream_node;
				}
			}
			// Connect the new downstream node to this node.
			__downstream = downstream_node;

			// Set the upstream node of the new downstream node to point to this node.
			// For now, assume that the node that is being inserted is a new node...
			if (downstream_node.getNumUpstreamNodes() > 0)
			{
				Message.printWarning(1, routine, "Node \"" + downstream_node.getCommonID() + "\" has #upstream > 0");
			}

			// Set the new downstream node data...
			downstream_node.setDownstreamNode(oldDownstreamNode);
			downstream_node.addUpstreamNode(this);
			// Set the new current node data...
			__tributaryNum = downstream_node.getNumUpstreamNodes();
		}
		else
		{
			// We always need to do this step...
			downstream_node.addUpstreamNode(this);
		}

		string downstreamCommonid = null;
		if (downstream_node.getDownstreamNode() != null)
		{
			downstreamCommonid = oldDownstreamNode.getCommonID();
		}
		if (Message.isDebugOn)
		{
			Message.printDebug(dl, routine, "\"" + downstream_node.getCommonID() + "\" is downstream of \"" + getCommonID() + "\" and upstream of \"" + downstreamCommonid + "\"");
		}
		return true;

		}
		catch (Exception e)
		{
			Message.printWarning(2,routine,"Error adding downstream node.");
			Message.printWarning(2, routine, e);
			return false;
		}
	}

	/// <summary>
	/// Adds a node upstream of this node.  This method is used by the network 
	/// plotting code and simply appends this node to the end of the __upstream 
	/// Vector, which is why it is used in lieu of the addUpstreamNode() method. </summary>
	/// <param name="node"> the node to add. </param>
	public virtual void addUpstream(HydrologyNode node)
	{
		__upstream.Add(node);
	}

	/// <summary>
	/// Add a node upstream from this node. </summary>
	/// <param name="upstream_node"> Node to add upstream. </param>
	/// <returns> true if successful, false if not. </returns>
	public virtual bool addUpstreamNode(HydrologyNode upstream_node)
	{
		string routine = __CLASS + ".addUpstreamNode";
		int dl = 50;

		// Add the node to the vector...
		try
		{
			if (Message.isDebugOn)
			{
				Message.printDebug(dl, routine, "Adding \"" + upstream_node.getCommonID() + "\" upstream of \"" + getCommonID() + "\"");
			}
			if (__upstream == null)
			{
				// Need to allocate space for it...
				__upstream = new List<HydrologyNode>();
			}
			__upstream.Add(upstream_node);

			// Make so the upstream node has this node as its downstream node...
			upstream_node.setDownstreamNode(this);
			if (Message.isDebugOn)
			{
				Message.printDebug(dl, routine, "\"" + upstream_node.getCommonID() + "\" downstream is \"" + getCommonID() + "\"");
			}

			return true;
		}
		catch (Exception e)
		{
			Message.printWarning(3, routine, "Error adding upstream node.");
			Message.printWarning(3, routine, e);
			return false;
		}
	}

	/// <summary>
	/// Adds an id to the list of upstream node ids.  Used by the network drawing code. </summary>
	/// <param name="id"> the id to add to the upstream node vector.  If the list is null it will first be created. </param>
	public virtual void addUpstreamNodeID(string id)
	{
		if (__upstreamNodeIDs == null)
		{
			__upstreamNodeIDs = new List<string>();
		}
		__upstreamNodeIDs.Add(id);
	}

	/// <summary>
	/// Calculates the extent occupied by this node on the drawing area. </summary>
	/// <param name="da"> the drawing area on which to calculate bounds. </param>
	public virtual void calculateExtents(GRJComponentDrawingArea da)
	{
		// FIXME SAM 2008-03-15 Need to remove WIS from this general class
		/*
		if (__inWis) {
			calculateWISBounds(da);
		}
		else {
		*/
			calculateNodeExtentForNetwork(da);
		/*
		}
		*/
	}

	/// <summary>
	/// Calculates extent of this node when rendered in the Network editor display. </summary>
	/// <param name="da"> the drawing area on which to calculate bounds. </param>
	private void calculateNodeExtentForNetwork(GRJComponentDrawingArea da)
	{
		__symText = null;
		__symbol = null;
		__nodeType = null;
		if (string.ReferenceEquals(__nodeType, null))
		{
			switch (__type)
			{
				case NODE_TYPE_BLANK:
					__nodeType = "Blank";
					break;
				case NODE_TYPE_DIV:
					__nodeType = "Diversion";
					break;
				case NODE_TYPE_FLOW:
					__nodeType = "Streamflow";
					break;
				case NODE_TYPE_CONFLUENCE:
					__nodeType = "Confluence";
					break;
				case NODE_TYPE_ISF:
					__nodeType = "Instream Flow";
					break;
				case NODE_TYPE_RES:
					__nodeType = "Reservoir";
					break;
				case NODE_TYPE_IMPORT:
					__nodeType = "Import";
					break;
				case NODE_TYPE_BASEFLOW:
					__nodeType = "Baseflow";
					break;
				case NODE_TYPE_END:
					__nodeType = "End";
					break;
				case NODE_TYPE_OTHER:
					__nodeType = "Other";
					break;
				case NODE_TYPE_UNKNOWN:
					__nodeType = "Unknown";
					break;
				case NODE_TYPE_STREAM:
					__nodeType = "StreamTop";
					break;
				case NODE_TYPE_LABEL:
					__nodeType = "Label";
					break;
				case NODE_TYPE_WELL:
					__nodeType = "Well";
					break;
				case NODE_TYPE_XCONFLUENCE:
					__nodeType = "XConfluence";
					break;
				case NODE_TYPE_DIV_AND_WELL:
					__nodeType = "DiversionAndWell";
					break;
				case NODE_TYPE_PLAN:
					__nodeType = "Plan";
					break;
				default:
					Message.printStatus(1, "", "Unrecognized node " + "type: " + __type);
					__nodeType = null;
					break;
			}
		}

		if (__symbol == null)
		{
			// Symbol has not been defined for the node so do it (and adjust the size if necessary)
			setSymbolFromNodeType(lookupType(__nodeType), true);
		}

		string label = __label;

		if (__symbol != null)
		{
			//double width = getIconDiameter();
			double height = getIconDiameter();

			if (!string.ReferenceEquals(__label, null))
			{
				GRLimits limits = GRDrawingAreaUtil.getTextExtents(da, label, GRUnits.DEVICE);
				if (limits.getHeight() > height)
				{
					height = limits.getHeight();
				}
				//width += 4 + limits.getWidth();
			}
		}

		__boundsCalculated = true;
	}

	/// <summary>
	/// Calculates bounds for nodes in the WIS Network display. </summary>
	/// <param name="da"> the drawing area on which to calculate bounds. </param>
	private void calculateWISBounds(GRJComponentDrawingArea da)
	{
		if (__symbol == null)
		{
			int style = GRSymbol.TYPE_POLYGON;
			GRColor black = GRColor.black;
			int iconDiameter = getIconDiameter();

			// The secondary symbol is used to blank out the area behind the primary symbol's node icon.  

			if (__nodeType.Equals("Reservoir", StringComparison.OrdinalIgnoreCase))
			{
				__symbol = new GRSymbol(GRSymbol.SYM_RTRI, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__secondarySymbol = new GRSymbol(GRSymbol.SYM_FRTRI, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__symText = null;
			}
			else if (__nodeType.Equals("Stream", StringComparison.OrdinalIgnoreCase))
			{
				__symbol = new GRSymbol(GRSymbol.SYM_CIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__secondarySymbol = new GRSymbol(GRSymbol.SYM_FCIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__symText = "O";
			}
			else if (__nodeType.Equals("Confluence", StringComparison.OrdinalIgnoreCase))
			{
				__symbol = new GRSymbol(GRSymbol.SYM_CIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__secondarySymbol = new GRSymbol(GRSymbol.SYM_FCIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__symText = "C";
			}
			else if (__nodeType.Equals("Station", StringComparison.OrdinalIgnoreCase) || __nodeType.Equals("Streamflow", StringComparison.OrdinalIgnoreCase))
			{
				__symbol = new GRSymbol(GRSymbol.SYM_CIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__secondarySymbol = new GRSymbol(GRSymbol.SYM_FCIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__symText = "B";
			}
			else if (__nodeType.Equals("Diversion", StringComparison.OrdinalIgnoreCase))
			{
				__symbol = new GRSymbol(GRSymbol.SYM_CIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__secondarySymbol = new GRSymbol(GRSymbol.SYM_FCIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__symText = "D";
			}
			else if (__nodeType.Equals("MinFlow", StringComparison.OrdinalIgnoreCase))
			{
				__symbol = new GRSymbol(GRSymbol.SYM_CIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__secondarySymbol = new GRSymbol(GRSymbol.SYM_FCIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__symText = "M";
			}
			else if (__nodeType.Equals("Other", StringComparison.OrdinalIgnoreCase))
			{
				__symbol = new GRSymbol(GRSymbol.SYM_CIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__secondarySymbol = new GRSymbol(GRSymbol.SYM_FCIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__symText = "O";
			}
			else if (__nodeType.Equals("Plan", StringComparison.OrdinalIgnoreCase))
			{
				__symbol = new GRSymbol(GRSymbol.SYM_CIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__secondarySymbol = new GRSymbol(GRSymbol.SYM_FSQ, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__symText = "PL";
			}
			else if (__nodeType.Equals("End", StringComparison.OrdinalIgnoreCase))
			{
				__symbol = new GRSymbol(GRSymbol.SYM_FCIR, style, black, black, iconDiameter * 2 / 3, iconDiameter * 2 / 3);
				__symText = null;
			}
			else
			{
				Message.printStatus(2, "", "Unknown node type: " + __nodeType);
			}
		}

		GRLimits data = da.getDataLimits();
		// TODO (JTS - 2004-05-19)
		// the following will need changed if we move from a scrollable area to a jumping area
		GRLimits draw = da.getDrawingLimits();

		// calculate the multiplier necessary to convert data units to device units.
		double mod = 1;
		if (draw.getWidth() > draw.getHeight())
		{
			mod = data.getWidth() / draw.getWidth();
		}
		else
		{
			mod = data.getHeight() / draw.getHeight();
		}

		__height = getIconDiameter() * 2 / 3 * mod;
		__width = getIconDiameter() * 2 / 3 * mod;

		__boundsCalculated = true;
	}

	/// <summary>
	/// Clears the upstream node ids stored in the __upstreamNodeIDs list.  The list is set to null.
	/// </summary>
	public virtual void clearUpstreamNodeIDs()
	{
		if (__upstreamNodeIDs != null)
		{
			__upstreamNodeIDs.Clear();
		}
	}

	/// <summary>
	/// Checks to see if the specified point is contained in the area of the node 
	/// symbol.  Only checks the symbol of the node along with a tiny area outside the symbol as well. </summary>
	/// <returns> true if the point is contained, false if not. </returns>
	public virtual bool contains(double x, double y)
	{
		if ((x >= __x - 2 - (__width / 2)) && (x <= (__x + (__width / 2) + 2)))
		{
			if ((y >= __y - 2 - (__width / 2)) && (y <= (__y + (__height / 2) + 2)))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Break the link with an upstream node. </summary>
	/// <param name="upstreamNode"> Upstream node to disconnect from the network. </param>
	/// <returns> true if successful, false if not. </returns>
	public virtual bool deleteUpstreamNode(HydrologyNode upstreamNode)
	{
		string routine = __CLASS + ".deleteUpstreamNode";

		// Find a matching node.  Just check addresses...
		try
		{

			for (int i = 0; i < __upstream.Count; i++)
			{
				if (upstreamNode.Equals(__upstream[i]))
				{
					// Found a match.  Delete the element...
					__upstream.RemoveAt(i);
					return true;
				}
			}
			return false;

		}
		catch (Exception)
		{
			Message.printWarning(2,routine,"Error deleting upstream node.");
			return false;
		}
	}

	/// <summary>
	/// Draws this node on the specified drawing area. </summary>
	/// <param name="da"> the drawing area on which to draw the node. </param>
	public virtual void draw(GRJComponentDrawingArea da)
	{
		if (!__boundsCalculated)
		{
			calculateExtents(da);
		}

		// FIXME SAM 2008-03-15 Need to remove WIS from this general class
		/*
		if (__inWis) {
			drawWIS(da);
		}
		else {
		*/
			drawNodeForNetwork(da);
			/*
		}
		*/
	}

	/// <summary>
	/// Draws this node on the network editor display. </summary>
	/// <param name="da"> the GRJComponentDrawingArea on which to draw the node. </param>
	private void drawNodeForNetwork(GRJComponentDrawingArea da)
	{
		string routine = "HydrologyNode.drawNodeForNetwork";

		double symbolSize = 0;
		// Symbol to be drawn as core - others may be drawn to decorate and reservoir symbol orientation
		// may be reset.
		int symbol = GRSymbol.SYM_NONE;

		// if there is a symbol to be drawn
		if (__symbol != null)
		{
			if (__symbol.getType() == GRSymbol.SYM_CIR)
			{
				// Fill in the background with white so the node can't be seen through.  For example, if
				// the symbol draws over something, the symbol needs to be clearly visible.
				GRDrawingAreaUtil.setColor(da, GRColor.white);
				GRDrawingAreaUtil.drawSymbol(da, GRSymbol.SYM_FCIR, __x, __y, __symbol.getSize(), GRUnits.DEVICE,0);
			}

			symbolSize = __symbol.getSize();

			if (__type == NODE_TYPE_RES)
			{
				// Need special care with reservoirs to orient the symbol the proper way.
				int labeldir = getLabelDirection() / 10;
				if (labeldir == 1)
				{
					symbol = GRSymbol.SYM_FUTRI;
				}
				else if (labeldir == 2)
				{
					symbol = GRSymbol.SYM_FDTRI;
				}
				else if (labeldir == 3)
				{
					symbol = GRSymbol.SYM_FLTRI;
				}
				else if (labeldir == 4)
				{
					symbol = GRSymbol.SYM_FRTRI;
				}
				else
				{
					symbol = GRSymbol.SYM_FRTRI;
				}
			}
			else
			{
				// The symbol does not need special attention - get it from the node data
				symbol = __symbol.getType();
			}
		}

		if (__drawText)
		{
			// There is a label (e.g., station ID) that needs to be drawn outside the symbol
			string label = null;
			int labelPos = 0;
			double labelAngle = 0;
			if ((__type == NODE_TYPE_BLANK) || (__type == NODE_TYPE_CONFLUENCE) || (__type == NODE_TYPE_XCONFLUENCE))
			{
				   // do nothing
			}
			else if (__type == NODE_TYPE_DIV || __type == NODE_TYPE_DIV_AND_WELL || __type == NODE_TYPE_WELL || __type == NODE_TYPE_IMPORT || __type == NODE_TYPE_FLOW || __type == NODE_TYPE_END || __type == NODE_TYPE_BASEFLOW || __type == NODE_TYPE_ISF || __type == NODE_TYPE_OTHER || __type == NODE_TYPE_RES || __type == NODE_TYPE_PLAN)
			{
				label = getNodeLabel(HydrologyNodeNetwork.LABEL_NODES_COMMONID);
			}
			else
			{
				Message.printWarning(3, routine, "No text specified!");
				label = "";
			}

			int dir = getLabelDirection();
			dir = dir % 10;

			if (dir == 1)
			{
				labelPos = GRText.BOTTOM | GRText.CENTER_X;
			}
			else if (dir == 7)
			{
				labelPos = GRText.BOTTOM | GRText.LEFT;
			}
			else if (dir == 4)
			{
				labelPos = GRText.LEFT | GRText.CENTER_Y;
			}
			else if (dir == 8)
			{
				labelPos = GRText.LEFT | GRText.TOP;
			}
			else if (dir == 2)
			{
				labelPos = GRText.TOP | GRText.CENTER_X;
			}
			else if (dir == 5)
			{
				labelPos = GRText.TOP | GRText.RIGHT;
			}
			else if (dir == 3)
			{
				labelPos = GRText.RIGHT | GRText.CENTER_Y;
			}
			else if (dir == 6)
			{
				labelPos = GRText.BOTTOM | GRText.RIGHT;
			}
			else if (dir == 9)
			{
				labelPos = GRText.CENTER_X | GRText.CENTER_Y;
			}
			else
			{
				labelPos = GRText.TOP | GRText.CENTER_X;
			}

			if (string.ReferenceEquals(label, null))
			{
				label = "";
			}

			// Draw the text without a symbol, at the position that is appropriate for the largest symbol
			// that will be shown.  Do not draw a symbol so that the logic below can be simpler and
			// to avoid redundant drawing.  Text should always be black.
			if (__isSelected)
			{
				// Draw in the selection color
				GRDrawingAreaUtil.setColor(da, GRColor.cyan);
			}
			else
			{
				// Draw in black
				GRDrawingAreaUtil.setColor(da, GRColor.black);
			}
			if (getIsNaturalFlow() || getIsImport())
			{
				// Draw the label text slightly offset because the decorator symbol takes up more room.
				Font oldFont = null;
				if (getIsNaturalFlow())
				{
					// Reset to bold...
					oldFont = da.getFont();
					da.setFont(oldFont.getName(), Font.BOLD, oldFont.getSize());
				}
				GRDrawingAreaUtil.drawSymbolText(da, GRSymbol.SYM_NONE, __x, __y, symbolSize + getDecoratorDiameter(), label, labelAngle, labelPos, GRUnits.DEVICE, 0);
				if (getIsNaturalFlow())
				{
					// Set back to old font.
					da.setFont(oldFont.getName(), oldFont.getStyle(), oldFont.getSize());
				}
			}
			else
			{
				// No need to offset the text - draw close to the normal symbol.
				GRDrawingAreaUtil.drawSymbolText(da, GRSymbol.SYM_NONE, __x, __y, symbolSize, label, labelAngle, labelPos, GRUnits.DEVICE, 0);
			}
		}

		// Draw the normal symbol

		if (__isSelected)
		{
			// Draw in the selection color
			GRDrawingAreaUtil.setColor(da, GRColor.cyan);
		}
		else
		{
			// Draw in black unless a different color has been specified for the symbol
			if (__symbol != null)
			{
				GRDrawingAreaUtil.setColor(da, __symbol.getColor());
			}
			else
			{
				GRDrawingAreaUtil.setColor(da, GRColor.black);
			}
		}
		//Message.printStatus(2, routine, "Drawing symbol " + symbol + " at " + __x + "," + __y + " size="+ symbolSize );
		GRDrawingAreaUtil.drawSymbol(da, symbol, __x, __y, symbolSize, GRUnits.DEVICE,0);

		// Draw a larger circle around decorator nodes
		if (__isSelected)
		{
			// Draw in the selection color
			GRDrawingAreaUtil.setColor(da, GRColor.cyan);
		}
		else
		{
			// Draw in black
			GRDrawingAreaUtil.setColor(da, GRColor.black);
		}
		if ((__symbol != null) && getIsNaturalFlow())
		{
			GRDrawingAreaUtil.drawSymbol(da, GRSymbol.SYM_CIR, __x, __y, __symbol.getSize() + getDecoratorDiameter(), GRUnits.DEVICE, 0);
		}

		// Draw a larger square around import nodes - same color as above
		if ((__symbol != null) && getIsImport())
		{
			//GRDrawingAreaUtil.drawSymbol(da, GRSymbol.SYM_FARR1,
			GRDrawingAreaUtil.drawSymbol(da, GRSymbol.SYM_SQ, __x, __y, __symbol.getSize() + getDecoratorDiameter(), GRUnits.DEVICE, 0);
		}

		// Draw the secondary symbol, for example the inner X in the end node..
		if (__secondarySymbol != null)
		{
			GRDrawingAreaUtil.drawSymbol(da, __secondarySymbol.getType(), __x, __y, __secondarySymbol.getSize(), GRUnits.DEVICE, 0);
		}

		// If used, draw text in the middle of the symbol (e.g., "D" for diversion node) - do last so
		// that it draws on top.  This should be in the select color or black.
		if (__isSelected)
		{
			// Draw in the selection color
			GRDrawingAreaUtil.setColor(da, GRColor.cyan);
		}
		else
		{
			// Draw in black
			GRDrawingAreaUtil.setColor(da, GRColor.black);
		}
		if (!string.ReferenceEquals(__symText, null))
		{
			GRDrawingAreaUtil.drawText(da, __symText, __x, __y, 0, GRText.CENTER_Y | GRText.CENTER_X);
		}
	}

	/// <summary>
	/// Draws this node for the WIS network display. </summary>
	/// <param name="da"> the GRJComponentDrawingArea on which to draw the node. </param>
	private void drawNodeForWIS(GRJComponentDrawingArea da)
	{
		// Format the label that accompanies the text
		string label = __label;
		if (__showDeliveryFlow)
		{
	//		label += " DF: " + StringUtil.formatString(__deliveryFlow, "%10.1f").trim();
		}
		if (__showNaturalFlow)
		{
	//		label += " NF: " + StringUtil.formatString(__naturalFlow, "%10.1f").trim();
		}
		if (__showPointFlow)
		{
			double pf = __pointFlow;
			if (!DMIUtil.isMissing(pf))
			{
				label += " PF: " + StringUtil.formatString(pf, "%10.1f").Trim();
			}
		}
		if (__showCalls)
		{
			if (!DMIUtil.isMissing(__call))
			{
				label += __call;
			}
		}
		if (__showRights)
		{
			if (!DMIUtil.isMissing(__right))
			{
				label += __right;
			}
		}

		if (__symbol != null)
		{
			GRDrawingAreaUtil.setColor(da, GRColor.white);
			GRDrawingAreaUtil.drawSymbol(da, __secondarySymbol.getType(), __x, __y, __secondarySymbol.getSize(), GRUnits.DEVICE, GRSymbol.SYM_CENTER_X | GRSymbol.SYM_CENTER_Y);

			// Stored in the node with a +1 modifier, necessary to ensure some backwards compatibility.
			int dir = getLabelDirection() - 1;
			if (getType() == NODE_TYPE_RES)
			{
				dir = dir % 10;
			}

			int labelPos = GRText.TOP;

			if (dir == 0)
			{ //  above
				labelPos = GRText.BOTTOM | GRText.CENTER_X;
			}
			else if (dir == 1)
			{ // below
				labelPos = GRText.TOP | GRText.CENTER_X;
			}
			else if (dir == 2)
			{ // center
				labelPos = GRText.CENTER_X | GRText.CENTER_Y;
			}
			else if (dir == 3)
			{ // left
				labelPos = GRText.RIGHT | GRText.CENTER_Y;
			}
			else if (dir == 4)
			{ // lower left
				labelPos = GRText.RIGHT | GRText.TOP;
			}
			else if (dir == 5)
			{ // lower right
				labelPos = GRText.LEFT | GRText.TOP;
			}
			else if (dir == 6)
			{ // right
				labelPos = GRText.LEFT | GRText.CENTER_Y;
			}
			else if (dir == 7)
			{ // upper left
				labelPos = GRText.RIGHT | GRText.BOTTOM;
			}
			else if (dir == 8)
			{ // upper right
				labelPos = GRText.LEFT | GRText.BOTTOM;
			}

			GRDrawingAreaUtil.setColor(da, GRColor.black);
			GRDrawingAreaUtil.drawSymbolText(da, __symbol.getType(), __x, __y, __symbol.getSize(), label, GRColor.black, 0, labelPos, GRUnits.DEVICE, GRSymbol.SYM_CENTER_X | GRSymbol.SYM_CENTER_Y);
		}

		if (!string.ReferenceEquals(__symText, null))
		{
			GRDrawingAreaUtil.drawText(da, __symText, __x, __y, 0, GRText.CENTER_Y | GRText.CENTER_X);
		}
	}

	/// <summary>
	/// Return a label to use for the specified node. </summary>
	/// <param name="lt"> Label type(see HydroBase_NodeNetwork.LABEL_NODES_*). </param>
	/// <returns> a label to use for the specified node. </returns>
	protected internal virtual string getNodeLabel(int lt)
	{
		string label = null;
		if (lt == HydrologyNodeNetwork.LABEL_NODES_AREA_PRECIP)
		{
			label = getAreaString() + "*" + getPrecipString();
		}
		else if (lt == HydrologyNodeNetwork.LABEL_NODES_COMMONID)
		{
			label = getCommonID();
		}
		else if (lt == HydrologyNodeNetwork.LABEL_NODES_PF)
		{
			if (getIsNaturalFlow() && (getType() != HydrologyNode.NODE_TYPE_FLOW))
			{
				label = StringUtil.formatString(getProrationFactor(), "%5.3f");
			}
			else
			{
				label = "";
			}
		}
		else if (lt == HydrologyNodeNetwork.LABEL_NODES_RIVERNODE)
		{
			label = getRiverNodeID();
		}
		else if (lt == HydrologyNodeNetwork.LABEL_NODES_WATER)
		{
			label = getWaterString();
		}
		else
		{
			label = getNetID();
		}
		return label;
	}

	/// <summary>
	/// Finalize before garbage collection.
	/// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected void finalize() throws Throwable
	~HydrologyNode()
	{
		__downstream = null;
		// FIXME SAM 2008-03-15 Need to remove WIS from this general class
		//__wisFormat = null;
		__associatedObject = null;
		__areaString = null;
		__desc = null;
		__commonID = null;
		__netID = null;
		__precipString = null;
		__riverNodeID = null;
		__userDesc = null;
		__waterString = null;
		__upstream = null;
		__symbol = null;
		__secondarySymbol = null;
		__call = null;
		__identifier = null;
		__label = null;
		__nodeType = null;
		__right = null;
		__symText = null;
		__downstreamNodeID = null;
		__upstreamNodeIDs = null;
//JAVA TO C# CONVERTER NOTE: The base class finalizer method is automatically called in C#:
//		base.finalize();
	}

	/// <summary>
	/// Returns the makenet area.  Makenet-specific. </summary>
	/// <returns> the makenet area. </returns>
	public virtual double getArea()
	{
		return __area;
	}

	/// <summary>
	/// Returns the makenet area as a String.  Makenet-specific. </summary>
	/// <returns> the makenet area as a String. </returns>
	public virtual string getAreaString()
	{
		return __areaString;
	}

	/// <summary>
	/// Returns the object associated with a node. </summary>
	/// <returns> the object associated with a node. </returns>
	public virtual object getAssociatedObject()
	{
		return __associatedObject;
	}

	/// <summary>
	/// Returns the node common id. </summary>
	/// <returns> the node common id. </returns>
	public virtual string getCommonID()
	{
		return __commonID;
	}

	/// <summary>
	/// Returns the computational order of nodes. </summary>
	/// <returns> the computational order of nodes. </returns>
	public virtual int getComputationalOrder()
	{
		return __computationalOrder;
	}

	/// <summary>
	/// Returns the diameter of the icon drawn for this node.  Used by the network plotting tools.
	/// TODO (JTS - 2004-05-20) might not be necessary -- many other methods -- would they work? </summary>
	/// <returns> the diameter of the icon drawn for this node. </returns>
	public virtual double getDataDiameter()
	{
		return __width;
	}

	/// <summary>
	/// Returns the decorator diameter. </summary>
	/// <returns> the decorator diameter. </returns>
	public virtual int getDecoratorDiameter()
	{
		return __decoratorDiameter;
	}

	/// <summary>
	/// Returns the delivery flow. </summary>
	/// <returns> the delivery flow. </returns>
	public virtual double getDeliveryFlow()
	{
		return __deliveryFlow;
	}

	/// <summary>
	/// Returns the node description. </summary>
	/// <returns> the node description. </returns>
	public virtual string getDescription()
	{
		return __desc;
	}

	/// <summary>
	/// Returns the UTM X value for the node's structure as stored in the database. </summary>
	/// <returns> the UTM X value for the node's structure as stored in the database. </returns>
	public virtual double getDBX()
	{
		return __dbX;
	}

	/// <summary>
	/// Returns the UTM Y value for the node's structure as stored in the database. </summary>
	/// <returns> the UTM Y value for the node's structure as stored in the database. </returns>
	public virtual double getDBY()
	{
		return __dbY;
	}

	/// <summary>
	/// Returns the node immediately downstream from this node. </summary>
	/// <returns> the node immediately downstream from this node. </returns>
	public virtual HydrologyNode getDownstreamNode()
	{
		return __downstream;
	}

	/// <summary>
	/// Returns the id of the node immediately downstream from this node.  Used by
	/// the network drawing code.  This method only returns the id stored in the
	/// __downstreamNodeID data member.  It doesn't check the downstream node to get
	/// the ID.  If setDownstreamNodeID() has not been called first, it will return null. </summary>
	/// <returns> the id of the node immediately downstream from this node. </returns>
	public virtual string getDownstreamNodeID()
	{
		return __downstreamNodeID;
	}

	/// <summary>
	/// Returns the point at which network connections should be drawn for the node.
	/// Currently only returns the x, y values. </summary>
	/// <returns> the point at which network connections should be drawn for the node. </returns>
	public virtual double[] getDrawToPoint(GRJComponentDrawingArea da)
	{
		double[] d = new double[2];
		d[0] = __x;
		d[1] = __y;
		return d;
	}

	/// <summary>
	/// Returns the height of the drawing area occupied by the node. </summary>
	/// <returns> the height of the drawing area occupied by the node. </returns>
	public virtual double getHeight()
	{
		return __height;
	}

	/// <summary>
	/// Returns the icon diameter. </summary>
	/// <returns> the icon diameter. </returns>
	public virtual int getIconDiameter()
	{
		return __iconDiameter;
	}

	/// <summary>
	/// Returns the network node drawer unique node identifier. </summary>
	/// <returns> the network node drawer unique node identifier. </returns>
	public virtual string getIdentifier()
	{
		return __identifier;
	}

	/// <summary>
	/// Returns whether this node is a dry river or not.  WIS-specific. </summary>
	/// <returns> whether this node is a dry river or not. </returns>
	public virtual bool getIsDryRiver()
	{
		return __isDryRiver;
	}

	/// <summary>
	/// Returns whether this node is an import node. </summary>
	/// <returns> whether this node is an import node. </returns>
	public virtual bool getIsImport()
	{
		return __isImport;
	}

	/// <summary>
	/// Returns whether this node is a natural flow node (attribute on node, not node type). </summary>
	/// <returns> whether this node is a natural flow node (attribute on node, not node type). </returns>
	public virtual bool getIsNaturalFlow()
	{
		return __isNaturalFlow;
	}

	/// <summary>
	/// Returns the label drawn for this node. </summary>
	/// <returns> the label drawn for this node. </returns>
	public virtual string getLabel()
	{
		if (string.ReferenceEquals(__label, null) || __label.Trim().Equals(""))
		{
			return "";
		}
		return __label;
	}

	/// <summary>
	/// Returns the angle at which to print the label.  Makenet-specific. </summary>
	/// <returns> the angle at which to print the label. </returns>
	public virtual double getLabelAngle()
	{
		return __labelAngle;
	}

	/// <summary>
	/// Returns the direction to print the label at.  Makenet-specific. </summary>
	/// <returns> the direction to print the label at. </returns>
	public virtual int getLabelDirection()
	{
		return __labelDir;
	}

	/// <summary>
	/// Returns the limits of the area occupied by the drawn node.  The limits only 
	/// encompass the area occupied by the node icon. </summary>
	/// <returns> the limits of the area occupied by the drawn node. </returns>
	public virtual GRLimits getLimits()
	{
		return new GRLimits(__x - (__width / 2), __y - (__height / 2), __x + (__width / 2), __y + (__height / 2));
	}

	/// <summary>
	/// Returns the link data for confluences. </summary>
	/// <returns> the link data for confluences. </returns>
	public virtual long getLink()
	{
		return __link;
	}

	/// <summary>
	/// Returns the natural flow. </summary>
	/// <returns> the natural flow. </returns>
	public virtual double getNaturalFlow()
	{
		return __naturalFlow;
	}

	/// <summary>
	/// Returns the ID from the net file.  Makenet-specific. </summary>
	/// <returns> the ID from the net file. </returns>
	public virtual string getNetID()
	{
		return __netID;
	}

	/// <summary>
	/// Returns the node number in the reach. </summary>
	/// <returns> the node number in the reach. </returns>
	public virtual int getNodeInReachNumber()
	{
		return __nodeInReachNum;
	}

	/// <summary>
	/// Returns the type of node this is. </summary>
	/// <returns> the type of node this is. </returns>
	public virtual string getNodeType()
	{
		return __nodeType;
	}

	/// <summary>
	/// Returns the number of upstream nodes. </summary>
	/// <returns> the number of upstream nodes. </returns>
	public virtual int getNumUpstreamNodes()
	{
		if (__upstream == null)
		{
			return 0;
		}
		else
		{
			return __upstream.Count;
		}
	}

	/// <summary>
	/// Returns the point flow. </summary>
	/// <returns> the point flow. </returns>
	public virtual double getPointFlow()
	{
		return __pointFlow;
	}

	/// <summary>
	/// Returns the precip.  Makenet-specific. </summary>
	/// <returns> the precip. </returns>
	public virtual double getPrecip()
	{
		return __precip;
	}

	/// <summary>
	/// Returns the precip as a String.  Makenet-specific. </summary>
	/// <returns> the precip as a String. </returns>
	public virtual string getPrecipString()
	{
		return __precipString;
	}

	/// <summary>
	/// Returns the gain proration factor.  Makenet-specific. </summary>
	/// <returns> the gain proration factor. </returns>
	public virtual double getProrationFactor()
	{
		return __prorationFactor;
	}

	/// <summary>
	/// Returns the reach number. </summary>
	/// <returns> the reach number. </returns>
	public virtual int getReachCounter()
	{
		return __reachCounter;
	}

	/// <summary>
	/// Returns the reach level. </summary>
	/// <returns> the reach level. </returns>
	public virtual int getReachLevel()
	{
		return __reachLevel;
	}

	/// <summary>
	/// Returns the river node ID.  Makenet-specific. </summary>
	/// <returns> the river node ID. </returns>
	public virtual string getRiverNodeID()
	{
		return __riverNodeID;
	}

	/// <summary>
	/// Returns the river node count. </summary>
	/// <returns> the river node count. </returns>
	public virtual int getSerial()
	{
		return __serial;
	}

	/// <summary>
	/// Returns the structure stream mile.  WIS-specific. </summary>
	/// <returns> the structure stream mile. </returns>
	public virtual double getStreamMile()
	{
		return __streamMile;
	}

	/// <summary>
	/// Returns the stream num.  WIS-specific. </summary>
	/// <returns> the stream num. </returns>
	public virtual long getStreamNumber()
	{
		return __streamNum;
	}

	/// <summary>
	/// Return the symbol used with the node.
	/// </summary>
	public virtual GRSymbol getSymbol()
	{
		return __symbol;
	}

	/// <summary>
	/// Returns the label direction in a format that a proplist will know -- one of
	/// the string values from GRText.  This assumes the string format is between 
	/// 1 and 9.  Any other values will result in "AboveCenter" being returned. </summary>
	/// <returns> the label direction in a string format. </returns>
	public virtual string getTextPosition()
	{
		// the text position as compared to the values in GRText is actually
		// stored +1 for some backwards compatibility issues.
		string[] positions = GRText.getTextPositions();
		if (__labelDir <= 0 || __labelDir > 9)
		{
			return positions[0];
		}
		else
		{
			return positions[(__labelDir - 1)];
		}
	}

	/// <summary>
	/// Returns the tributary number. </summary>
	/// <returns> the tributary number. </returns>
	public virtual int getTributaryNumber()
	{
		return __tributaryNum;
	}

	/// <summary>
	/// Returns the node type. </summary>
	/// <returns> the node type. </returns>
	public virtual int getType()
	{
		return __type;
	}

	/// <summary>
	/// Returns the elements in the __upstreamNodeIDs list as an array of Strings.
	/// The list contains the IDs of all the nodes immediately upstream from this
	/// node.  This is used by network drawing code. </summary>
	/// <returns> a String array of all the ids of nodes immediately upstream from this node. </returns>
	public virtual string[] getUpstreamNodeIDs()
	{
		int size = 0;
		if (__upstreamNodeIDs != null)
		{
			size = __upstreamNodeIDs.Count;
		}
		string[] ids = new string[size];
		for (int i = 0; i < size; i++)
		{
			ids[i] = __upstreamNodeIDs[i];
		}
		return ids;
	}

	/// <summary>
	/// Returns the IDs of all the nodes immediately upstream of this node.  Used by
	/// drawing code.  This goes to each of the upstream nodes in the __upstream 
	/// list and pulls out their ID to place in the String. </summary>
	/// <returns> a String array of all the nodes' ids in the order the nodes are found in the __upstream list. </returns>
	public virtual string[] getUpstreamNodesIDs()
	{
		if (__upstream == null || __upstream.Count == 0)
		{
			return new string[0];
		}

		string[] ids = new string[__upstream.Count];
		for (int i = 0; i < __upstream.Count; i++)
		{
			ids[i] = __upstream[i].getCommonID();
		}
		return ids;
	}

	/// <summary>
	/// Returns the user description.  Makenet-specific. </summary>
	/// <returns> the user description. </returns>
	public virtual string getUserDescription()
	{
		return __userDesc;
	}

	/// <summary>
	/// Returns the String node type from the integer type. </summary>
	/// <param name="type"> the type of the node. </param>
	/// <param name="flag"> if FULL, return the full String (used in the network file).  If
	/// ABBREVIATION, return the 3 letter abbreviation (used in StateMod station names). </param>
	/// <returns> the String node type from the integer type. </returns>
	public static string getTypeString(int type, int flag)
	{
		string stype = "";

		if (flag == ABBREVIATION)
		{
			// Abbreviations...
			if (type == NODE_TYPE_BLANK)
			{
				stype = "BLK";
			}
			else if (type == NODE_TYPE_DIV)
			{
				stype = "DIV";
			}
			else if (type == NODE_TYPE_DIV_AND_WELL)
			{
				stype = "D&W";
			}
			else if (type == NODE_TYPE_WELL)
			{
				stype = "WEL";
			}
			else if (type == NODE_TYPE_FLOW)
			{
				stype = "FLO";
			}
			else if (type == NODE_TYPE_CONFLUENCE)
			{
				stype = "CON";
			}
			else if (type == NODE_TYPE_ISF)
			{
				stype = "ISF";
			}
			else if (type == NODE_TYPE_RES)
			{
				stype = "RES";
			}
			else if (type == NODE_TYPE_IMPORT)
			{
				stype = "IMP";
			}
			else if (type == NODE_TYPE_BASEFLOW)
			{
				stype = "BFL";
			}
			else if (type == NODE_TYPE_END)
			{
				stype = "END";
			}
			else if (type == NODE_TYPE_OTHER)
			{
				stype = "OTH";
			}
			else if (type == NODE_TYPE_UNKNOWN)
			{
				stype = "UNK";
			}
			else if (type == NODE_TYPE_STREAM)
			{
				stype = "STR";
			}
			else if (type == NODE_TYPE_LABEL)
			{
				stype = "LAB";
			}
			else if (type == NODE_TYPE_FORMULA)
			{
				stype = "FOR";
			}
			else if (type == NODE_TYPE_XCONFLUENCE)
			{
				stype = "XCN";
			}
			else if (type == NODE_TYPE_LABEL_NODE)
			{
				stype = "LBN";
			}
			else if (type == NODE_TYPE_PLAN)
			{
				stype = "PLN";
			}
		}
		else
		{
			// Full name...
			if (type == NODE_TYPE_BLANK)
			{
				stype = "BLANK";
			}
			else if (type == NODE_TYPE_DIV)
			{
				stype = "DIV";
			}
			else if (type == NODE_TYPE_DIV_AND_WELL)
			{
				stype = "D&W";
			}
			else if (type == NODE_TYPE_WELL)
			{
				stype = "WELL";
			}
			else if (type == NODE_TYPE_FLOW)
			{
				stype = "FLOW";
			}
			else if (type == NODE_TYPE_CONFLUENCE)
			{
				stype = "CONFL";
			}
			else if (type == NODE_TYPE_ISF)
			{
				stype = "ISF";
			}
			else if (type == NODE_TYPE_RES)
			{
				stype = "RES";
			}
			else if (type == NODE_TYPE_IMPORT)
			{
				stype = "IMPORT";
			}
			else if (type == NODE_TYPE_BASEFLOW)
			{
				stype = "BFL";
			}
			else if (type == NODE_TYPE_END)
			{
				stype = "END";
			}
			else if (type == NODE_TYPE_OTHER)
			{
				stype = "OTH";
			}
			else if (type == NODE_TYPE_UNKNOWN)
			{
				stype = "UNKNOWN";
			}
			else if (type == NODE_TYPE_STREAM)
			{
				stype = "STREAM";
			}
			else if (type == NODE_TYPE_LABEL)
			{
				stype = "LABEL";
			}
			else if (type == NODE_TYPE_FORMULA)
			{
				stype = "FORMULA";
			}
			else if (type == NODE_TYPE_XCONFLUENCE)
			{
				stype = "XCONFL";
			}
			else if (type == NODE_TYPE_LABEL_NODE)
			{
				stype = "LABELNODE";
			}
			else if (type == NODE_TYPE_PLAN)
			{
				stype = "PLAN";
			}
		}
		return stype;
	}

	/// <summary>
	/// Returns the first upstream node. </summary>
	/// <returns> the reference to the first upstream node or null if not found. </returns>
	public virtual HydrologyNode getUpstreamNode()
	{
		if (__upstream == null)
		{
			return null;
		}
		// Return the first one...
		return getUpstreamNode(0);
	}

	/// <summary>
	/// Return the upstream node at the specific position. </summary>
	/// <param name="position"> 0-index position of upstream node. </param>
	/// <returns> the reference to the upstream node for the specified position, or null if not found. </returns>
	public virtual HydrologyNode getUpstreamNode(int position)
	{
		string routine = __CLASS + ".getUpstreamNode";

		if (__upstream == null)
		{
			return null;
		}
		if (__upstream.Count < (position + 1))
		{
			Message.printWarning(1, routine, "Upstream position [" + position + "] is not found(max " + __upstream.Count + ")");
			return null;
		}
		// Return the requested one...
		return (HydrologyNode)__upstream[position];
	}

	/// <summary>
	/// Returns an upstream node given its common id. </summary>
	/// <param name="commonID"> the commonID for which to find an upstream node. </param>
	/// <returns> an upstream node position given its common ID, or -1 if not found. </returns>
	public virtual int getUpstreamNodePosition(string commonID)
	{
		if (__upstream == null)
		{
			return -1;
		}
		int size = __upstream.Count;
		HydrologyNode upstream;
		for (int i = 0; i < size; i++)
		{
			// Return the first one that matches...
			upstream = __upstream[i];
			if (commonID.Equals(upstream.getCommonID(), StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return -1;
	}

	/// <summary>
	/// Returns the list of upstream nodes.  The internal list is returned (not a new reference). </summary>
	/// <returns> the list of upstream nodes. </returns>
	public virtual IList<HydrologyNode> getUpstreamNodes()
	{
		return __upstream;
	}

	/// <summary>
	/// Returns how upstream nodes are constructed.  See TRIBS_*. </summary>
	/// <returns> how upstream nodes are constructed. </returns>
	public virtual int getUpstreamOrder()
	{
		return __upstreamOrder;
	}

	/// <summary>
	/// Returns the verbose node type description string. </summary>
	/// <param name="type"> the type of node for which to return the verbose type. </param>
	/// <returns> the verbose node type description string. </returns>
	public static string getVerboseType(int type)
	{
		string stype = "";
		// Abbreviations...
		if (type == NODE_TYPE_BLANK)
		{
			stype = "Blank";
		}
		else if (type == NODE_TYPE_DIV)
		{
			stype = "Diversion";
		}
		else if (type == NODE_TYPE_DIV_AND_WELL)
		{
			stype = "Diversion and Well";
		}
		else if (type == NODE_TYPE_WELL)
		{
			stype = "Well";
		}
		else if (type == NODE_TYPE_FLOW)
		{
			stype = "Streamflow";
		}
		else if (type == NODE_TYPE_CONFLUENCE)
		{
			stype = "Confluence";
		}
		else if (type == NODE_TYPE_ISF)
		{
			stype = "Instream Flow";
		}
		else if (type == NODE_TYPE_RES)
		{
			stype = "Reservoir";
		}
		else if (type == NODE_TYPE_IMPORT)
		{
			stype = "Import";
		}
		else if (type == NODE_TYPE_BASEFLOW)
		{
			stype = "Baseflow";
		}
		else if (type == NODE_TYPE_END)
		{
			stype = "End";
		}
		else if (type == NODE_TYPE_OTHER)
		{
			stype = "Other";
		}
		else if (type == NODE_TYPE_UNKNOWN)
		{
			stype = "Uknown";
		}
		else if (type == NODE_TYPE_STREAM)
		{
			stype = "Stream";
		}
		else if (type == NODE_TYPE_LABEL)
		{
			stype = "Label";
		}
		else if (type == NODE_TYPE_FORMULA)
		{
			stype = "Formula";
		}
		else if (type == NODE_TYPE_XCONFLUENCE)
		{
			stype = "XConfluence";
		}
		else if (type == NODE_TYPE_LABEL_NODE)
		{
			stype = "LabelNode";
		}
		else if (type == NODE_TYPE_PLAN)
		{
			stype = "Plan";
		}
		return stype;
	}

	/// <summary>
	/// Returns the verbose node type description string. </summary>
	/// <param name="type"> the type of node for which to return the verbose type. </param>
	/// <returns> the verbose node type description string. </returns>
	public static string getVerboseWISType(int type)
	{
		string stype = "";
		// Abbreviations...
		if (type == NODE_TYPE_BLANK)
		{
			stype = "Blank";
		}
		else if (type == NODE_TYPE_DIV)
		{
			stype = "Diversion";
		}
		else if (type == NODE_TYPE_DIV_AND_WELL)
		{
			stype = "Diversion and Well";
		}
		else if (type == NODE_TYPE_WELL)
		{
			stype = "Well";
		}
		else if (type == NODE_TYPE_FLOW)
		{
			stype = "Station";
		}
		else if (type == NODE_TYPE_CONFLUENCE)
		{
			stype = "Confluence";
		}
		else if (type == NODE_TYPE_ISF)
		{
			stype = "Instream Flow";
		}
		else if (type == NODE_TYPE_RES)
		{
			stype = "Reservoir";
		}
		else if (type == NODE_TYPE_IMPORT)
		{
			stype = "Import";
		}
		else if (type == NODE_TYPE_BASEFLOW)
		{
			stype = "Baseflow";
		}
		else if (type == NODE_TYPE_END)
		{
			stype = "End";
		}
		else if (type == NODE_TYPE_OTHER)
		{
			stype = "Other";
		}
		else if (type == NODE_TYPE_UNKNOWN)
		{
			stype = "Uknown";
		}
		else if (type == NODE_TYPE_STREAM)
		{
			stype = "Stream";
		}
		else if (type == NODE_TYPE_LABEL)
		{
			stype = "Label";
		}
		else if (type == NODE_TYPE_FORMULA)
		{
			stype = "Formula";
		}
		else if (type == NODE_TYPE_XCONFLUENCE)
		{
			stype = "XConfluence";
		}
		else if (type == NODE_TYPE_LABEL_NODE)
		{
			stype = "LabelNode";
		}
		else if (type == NODE_TYPE_PLAN)
		{
			stype = "Plan";
		}
		return stype;
	}

	/// <summary>
	/// Returns area * precip as a float.  Makenet-specific. </summary>
	/// <returns> area * precip. </returns>
	public virtual double getWater()
	{
		return __water;
	}

	/// <summary>
	/// Returns area * precip as a String.  Makenet-specific. </summary>
	/// <returns> area * precip as a String. </returns>
	public virtual string getWaterString()
	{
		return __waterString;
	}

	/// <summary>
	/// Returns the width of the drawing area occupied by this node. </summary>
	/// <returns> the width of the drawing area occupied by this node. </returns>
	public virtual double getWidth()
	{
		return __width;
	}

	/// <summary>
	/// Returns the wis format associated with this node. </summary>
	/// <returns> the wis format associated with this node. </returns>
	/*
	public HydroBase_WISFormat getWISFormat() {
		return __wisFormat;
	}
	*/
	//FIXME SAM 2008-03-15 Need to remove WIS from this general class

	/// <summary>
	/// Returns the wis num associated with this node. </summary>
	/// <returns> the wis num associated with this node. </returns>
	/*
	public int getWis_num() {
		return __wisNum;
	}
	*/
	//FIXME SAM 2008-03-15 Need to remove WIS from this general class

	/// <summary>
	/// Returns the node X coordinate.  Makenet-specific. </summary>
	/// <returns> the node X coordinate. </returns>
	public virtual double getX()
	{
		return __x;
	}

	/// <summary>
	/// Returns the node Y coordinate.  Makenet-specific. </summary>
	/// <returns> the node Y coordinate. </returns>
	public virtual double getY()
	{
		return __y;
	}

	/// <summary>
	/// Inserts an upstream node into the Vector of upstream nodes.  Used by the network diagramming tools. </summary>
	/// <param name="node"> the node to insert. </param>
	/// <param name="pos"> the position at which to insert the node. </param>
	public virtual void insertUpstreamNode(HydrologyNode node, int pos)
	{
		__upstream.Insert(pos,node);
	}

	/// <summary>
	/// Inserts multiple nodes into the list of upstream nodes.  Used by network diagramming tools. </summary>
	/// <param name="nodes"> a non-null list of nodes to be inserted upstream of this node. </param>
	/// <param name="pos"> the position at which to insert the nodes. </param>
	public virtual void insertUpstreamNodes(IList<HydrologyNode> nodes, int pos)
	{
		for (int i = nodes.Count - 1; i >= 0; i--)
		{
			__upstream.Insert(pos,nodes[i]);
		}
	}

	/// <summary>
	/// Initialize data members.
	/// </summary>
	private void initialize()
	{
		__areaString = "";
		__area = 0.0;
		__precipString = "";
		__precip = 0.0;
		__waterString = "";
		__water = 0.0;
		__prorationFactor = 0.0;
		// TODO SAM 2011-01-05 Evaluate whether missing values should be NaN
		__x = 0.0;
		__y = 0.0;
		__labelAngle = 45;

		__desc = "";
		__userDesc = "";

		__commonID = "";
		__netID = "";
		__riverNodeID = "";
		__isNaturalFlow = false;
		__labelDir = 1;
		__serial = 0;
		__computationalOrder = -1;
		__type = NODE_TYPE_BLANK;
		__tributaryNum = 1;
		__reachCounter = 0;
		__reachLevel = 0;
		__nodeInReachNum = 1;
		__downstream = null;
		__upstream = null;
		__upstreamOrder = TRIBS_ADDED_FIRST;

		// WIS data...
		__streamMile = DMIUtil.MISSING_DOUBLE;
		__streamNum = DMIUtil.MISSING_LONG;
		// FIXME SAM 2008-03-15 Need to remove WIS from this general class
		//__wisFormat = 		null;
		__link = DMIUtil.MISSING_LONG;
		__isDryRiver = false;
	}

	/// <summary>
	/// Returns whether this node was read from the database or generated from a 
	/// network during the latest invocation of the network drawing code. </summary>
	/// <returns> whether this node was read from the database or not. </returns>
	public virtual bool isReadFromDB()
	{
		return __readFromDB;
	}

	/// <summary>
	/// Returns whether this node was selected on the network diagram. </summary>
	/// <returns> whether this node was selected on the netowrk diagram. </returns>
	public virtual bool isSelected()
	{
		return __isSelected;
	}

	/// <summary>
	/// Returns whether this node is visible or not. </summary>
	/// <returns> whether this node is visible or not. </returns>
	public virtual bool isVisible()
	{
		return __visible;
	}

	/// <summary>
	/// Returns whether this node is within the specified limits or not. </summary>
	/// <param name="limits"> the limits to check </param>
	/// <param name="scale"> the scale value of the drawing area </param>
	/// <returns> true if the node is within the limits, false if not. </returns>
	public virtual bool isWithinLimits(GRLimits limits, double scale)
	{
		return limits.contains(__x * scale, __y * scale, (__x + __width) * scale, (__y + __height) * scale, false);
	}

	/// <summary>
	/// Lookup the type from a string.  The string can be any recognized full or
	/// abbreviated string used for the network or CWRAT.  It can also be the
	/// abbreviated string used in StateMod station names and node diagram. </summary>
	/// <returns> the integer node type or -1 if not recognized. </returns>
	public static int lookupType(string typeString)
	{
		if (typeString.Equals("BFL", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_BASEFLOW;
		}
		else if (typeString.Equals("BLANK", StringComparison.OrdinalIgnoreCase) || typeString.Equals("BLK", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_BLANK;
		}
		else if (typeString.Equals("confluence", StringComparison.OrdinalIgnoreCase) || typeString.Equals("CON", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_CONFLUENCE;
		}
		else if (typeString.Equals("diversion", StringComparison.OrdinalIgnoreCase) || typeString.Equals("DIV", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_DIV;
		}
		else if (typeString.Equals("D&W", StringComparison.OrdinalIgnoreCase) || typeString.Equals("DW", StringComparison.OrdinalIgnoreCase) || typeString.Equals("DiversionAndWell", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_DIV_AND_WELL;
		}
		else if (typeString.Equals("END", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_END;
		}
		else if (typeString.Equals("FLOW", StringComparison.OrdinalIgnoreCase) || typeString.Equals("FLO", StringComparison.OrdinalIgnoreCase) || typeString.Equals("Streamflow", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_FLOW;
		}
		else if (typeString.Equals("formula", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_FORMULA;
		}
		else if (typeString.Equals("ISF", StringComparison.OrdinalIgnoreCase) || typeString.Equals("Instream Flow", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_ISF;
		}
		else if (typeString.Equals("IMP", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_IMPORT;
		}
		else if (typeString.Equals("LabelNode", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_LABEL_NODE;
		}
		else if (typeString.Equals("minflow", StringComparison.OrdinalIgnoreCase) || typeString.Equals("ISF", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_ISF;
		}
		else if (typeString.Equals("other", StringComparison.OrdinalIgnoreCase) || typeString.Equals("OTH", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_OTHER;
		}
		else if (typeString.Equals("Plan", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_PLAN;
		}
		else if (typeString.Equals("reservoir", StringComparison.OrdinalIgnoreCase) || typeString.Equals("RES", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_RES;
		}
		else if (typeString.Equals("station", StringComparison.OrdinalIgnoreCase) || typeString.Equals("FLO", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_FLOW;
		}
		else if (typeString.Equals("stream", StringComparison.OrdinalIgnoreCase) || typeString.Equals("STR", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_STREAM;
		}
		else if (typeString.Equals("string", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_LABEL;
		}
		else if (typeString.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_UNKNOWN;
		}
		else if (typeString.Equals("well", StringComparison.OrdinalIgnoreCase) || typeString.Equals("WEL", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_WELL;
		}
		else if (typeString.Equals("XCONFL", StringComparison.OrdinalIgnoreCase) || typeString.Equals("XCN", StringComparison.OrdinalIgnoreCase))
		{
			return NODE_TYPE_XCONFLUENCE;
		}
		else
		{
			string routine = "HydrologyNode.lookupType";
			Message.printWarning(3, routine, "Unable to convert node type \"" + typeString + "\" to internal type.");
			return -1;
		}
	}

	/// <summary>
	/// Breaks apart proration information and saves in the node. </summary>
	/// <param name="string0"> the input string.  Assumed to be either xxx*yyy or xxxx. </param>
	/// <returns> true if successful, false if not. </returns>
	public virtual bool parseAreaPrecip(string string0)
	{
		string routine = __CLASS + ".getNodeAreaPrecip";

		// If the proration information is empty, use empty strings for other parts...
		try
		{
		setAreaString("");
		setPrecipString("");
		setWaterString("");

		if (string.ReferenceEquals(string0, null))
		{
			return true;
		}
		if (string0.Length == 0)
		{
			return true;
		}

		string @string = string0;
		char theOperator = '\0';
		int length = @string.Length;
		int nfields = 1;
		for (int i = 0; i < length; i++)
		{
			if ((@string[i] == '-') || (@string[i] == '*') || (@string[i] == '+') || (@string[i] == '/'))
			{
				theOperator = @string[i];
				nfields = 2;
				// Set to a space so that we can parse later...
				@string = @string.Substring(0,i) + " " + @string.Substring((i + 1));
				if (Message.isDebugOn)
				{
					Message.printDebug(10, routine, "String=\"" + @string + "\"");
				}
			}
		}

		string area = "";
		string precip = "";
		if (nfields == 1)
		{
			// Just use the original string but set the precip to one so that we can print it if we want...
			setAreaString(string0);
			setPrecipString("1");
			setWaterString(string0);
			return true;
		}
		else if (nfields == 2)
		{
			// Assume that we have a valid theOperator and do the math...
			IList<string> v = StringUtil.breakStringList(@string, " \t", 0);
			area = v[0];
			precip = v[1];
			double a = (Convert.ToDouble(area));
			double p = (Convert.ToDouble(precip));
			double water = 0;
			if (theOperator == '*')
			{
				water = a * p;
			}
			else if ((theOperator == '/') || (theOperator == '+') || (theOperator == '-'))
			{
				Message.printWarning(1, routine, "Operator " + theOperator + " not allowed - use * for proration");
				return false;
				//water = a/p;
				//water = a + p;
				//water = a - p;
			}
			else
			{
				Message.printWarning(1, routine, "String \"" + string0 + "\" cannot be converted to proration");
				return false;
			}
			// Now save values in the node in both string and floating point form...
			setAreaString(area);
			setPrecipString(precip);
			setWaterString(StringUtil.formatString(water, "%12.2f"));
			setArea((Convert.ToDouble(area)));
			setPrecip((Convert.ToDouble(precip)));
			setWater(water);
			if (getWater() > 0.0)
			{
				// This is a natural flow node...
				setIsNaturalFlow(true);
			}
			return true;
		}

		Message.printWarning(1, routine, "String \"" + string0 + "\" cannot be converted to proration");
		return false;

		}
		catch (Exception e)
		{
			Message.printWarning(1, routine, "String \"" + string0 + "\" cannot be converted to proration");
			Message.printWarning(2, routine, e);
			return false;
		}
	}

	/// <summary>
	/// Removes a node from the upstream of this node.  Used by the network drawing code. </summary>
	/// <param name="pos"> the position in the __upstream list of the node to be removed. </param>
	public virtual void removeUpstreamNode(int pos)
	{
		__upstream.RemoveAt(pos);
	}

	/// <summary>
	/// Replaces one of this node's upstream nodes with another node.  Used by the network drawing code. </summary>
	/// <param name="node"> the node to replace the upstream node with. </param>
	/// <param name="pos"> the position in the __upstream list of the node to be replaced. </param>
	public virtual void replaceUpstreamNode(HydrologyNode node, int pos)
	{
		__upstream[pos] = node;
	}

	/// <summary>
	/// Reset some internal node things when reusing a node (for use in the network editor layout).
	/// This seems to be used only by the legend drawing, but is retained for utility.
	/// The following resets are done:
	/// <ol>
	/// <li>verbose node type (string) is looked up from the node type</li>
	/// <li>symbol is set to the default for the type, considering the natural flow and import flags</li>
	/// <li>secondary symbol is set to null</li>
	/// <li>boundscCalculated is set to false</li>
	/// </ol> </summary>
	/// <param name="type"> the new type to assign the node </param>
	/// <param name="isNaturalFlow"> indicates whether the node is a baseflow node </param>
	/// <param name="isImport"> indicates whether the node is an import node </param>
	public virtual void resetNode(int type, bool isNaturalFlow, bool isImport)
	{
		__type = type;
		__boundsCalculated = false;
		__isNaturalFlow = isNaturalFlow;
		__isImport = isImport;
		__nodeType = getTypeString(type, FULL);
		__secondarySymbol = null;
		setSymbolFromNodeType(type, false);
	}

	/// <summary>
	/// Sets the makenet area. </summary>
	/// <param name="area"> value to put in the makenet area. </param>
	public virtual void setArea(double area)
	{
		__area = area;
	}

	/// <summary>
	/// Sets the makenet area as a String. </summary>
	/// <param name="areaString"> value to put into the makenet area String. </param>
	public virtual void setAreaString(string areaString)
	{
		if (!string.ReferenceEquals(areaString, null))
		{
			__areaString = areaString;
		}
	}

	/// <summary>
	/// Sets the object associated with this node. </summary>
	/// <param name="o"> the object associated with the node. </param>
	public virtual void setAssociatedObject(object o)
	{
		__associatedObject = o;
	}

	/// <summary>
	/// Sets whether the bounds have been calculated or not.  If set to false, then
	/// the next time the node is drawn it will recalculate all its bounds. </summary>
	/// <param name="calculated"> whether the drawing bounds have been calculated or not. </param>
	public virtual void setBoundsCalculated(bool calculated)
	{
		__boundsCalculated = calculated;
	}

	/// <summary>
	/// Sets the call information to be displayed by the node label. </summary>
	/// <param name="call"> the information to display. </param>
	public virtual void setCall(string call)
	{
		__call = call;
	}

	/// <summary>
	/// Sets the node's common id. </summary>
	/// <param name="commonid"> the value to put into the node's common id. </param>
	public virtual void setCommonID(string commonid)
	{
		if (!string.ReferenceEquals(commonid, null))
		{
			__commonID = commonid;
		}
	}

	/// <summary>
	/// Sets the computational order of nodes. </summary>
	/// <param name="computationalOrder"> the value to set the computation order to. </param>
	public virtual void setComputationalOrder(int computationalOrder)
	{
		__computationalOrder = computationalOrder;
	}

	/// <summary>
	/// Sets the diameter of the node in data units (for use in determing what points are contained). </summary>
	/// <param name="diam"> the diam of the node in data units. </param>
	public virtual void setDataDiameter(double diam)
	{
		__width = diam;
		__height = diam;
	}

	/// <summary>
	/// Sets the UTM X value stored in the database. </summary>
	/// <param name="x"> the x value to set. </param>
	public virtual void setDBX(double x)
	{
		__dbX = x;
	}

	/// <summary>
	/// Sets the UTM Y value stored in the database. </summary>
	/// <param name="y"> the y value to set. </param>
	public virtual void setDBY(double y)
	{
		__dbY = y;
	}

	/// <summary>
	/// Sets the decorator diameter.  The decorator icon diameter will be computed accordingly. </summary>
	/// <param name="decoratorDiameter"> size of the decorator diameter in pixels.   </param>
	public virtual void setDecoratorDiameter(int decoratorDiameter)
	{
		__decoratorDiameter = decoratorDiameter;
	}

	/// <summary>
	/// Sets the delivery flow. </summary>
	/// <param name="deliveryFlow"> value to put in the delivery flow. </param>
	public virtual void setDeliveryFlow(double deliveryFlow)
	{
		__deliveryFlow = deliveryFlow;
	}

	/// <summary>
	/// Sets the description based on the node type.
	/// </summary>
	public virtual void setDescription()
	{
		setDescription(getTypeString(__type,1));
	}

	/// <summary>
	/// Sets the node description. </summary>
	/// <param name="desc"> the node description to store in the node. </param>
	public virtual void setDescription(string desc)
	{
		if (!string.ReferenceEquals(desc, null))
		{
			__desc = desc;
		}
	}

	/// <summary>
	/// Sets the node immediately downstream from this node. </summary>
	/// <param name="downstream"> the node immediately downstream from this node. </param>
	public virtual void setDownstreamNode(HydrologyNode downstream)
	{
		__downstream = downstream;
	}

	/// <summary>
	/// Sets the id of the node downstream from this node.  Used in the network drawing code. </summary>
	/// <param name="id"> the id of the node downstream from this node. </param>
	public virtual void setDownstreamNodeID(string id)
	{
		__downstreamNodeID = id;
	}

	/// <summary>
	/// Sets whether text labels should be drawn along with the node in the network editor. </summary>
	/// <param name="drawText"> whether to draw text labels. </param>
	public static void setDrawText(bool drawText)
	{
		__drawText = drawText;
	}

	/// <summary>
	/// Sets the icon diameter.  The decorator icon diameter will be computed accordingly. </summary>
	/// <param name="iconDiameter"> size of the icon diameter in drawing units (points for printing, pixels for screen).   </param>
	public virtual void setIconDiameter(int iconDiameter)
	{
		__iconDiameter = iconDiameter;
		int third = __iconDiameter / 3;
		if ((third % 2) == 1)
		{
			third++;
		}
		setDecoratorDiameter(third);
	}

	/// <summary>
	/// Sets the node drawing code identifier for the node. </summary>
	/// <param name="identifier"> the identifier to set. </param>
	public virtual void setIdentifier(string identifier)
	{
		__identifier = identifier;
	}

	/// <summary>
	/// Set whether the node is being drawn in the WIS network display. </summary>
	/// <param name="inWis"> whether the node is being drawn in the WIS network display. </param>
	/*
	public void setInWis(boolean inWis) {
		__inWis = inWis;
	}
	*/
	//FIXME SAM 2008-03-15 Need to remove WIS from this general class

	/*
	public void setInWIS(boolean inWis) {
		__inWis = inWis;
	}
	*/
	//FIXME SAM 2008-03-15 Need to remove WIS from this general class

	/// <summary>
	/// Sets whether this node is a dry river or not.  WIS-specific. </summary>
	/// <param name="isDryRiver"> whether this node is a dry river or not. </param>
	public virtual void setIsDryRiver(bool isDryRiver)
	{
		__isDryRiver = isDryRiver;
	}

	/// <summary>
	/// Sets whether this node is an import node or not. </summary>
	/// <param name="isImport"> whether this node is an import node or not. </param>
	public virtual void setIsImport(bool isImport)
	{
		__isImport = isImport;
	}

	/// <summary>
	/// Sets whether this node is a natural flow node or not. </summary>
	/// <param name="isNaturalFlow"> whether this node is a natural flow node or not. </param>
	public virtual void setIsNaturalFlow(bool isNaturalflow)
	{
		__isNaturalFlow = isNaturalflow;
	}

	/// <summary>
	/// Sets the node label. </summary>
	/// <param name="label"> the label to set. </param>
	public virtual void setLabel(string label)
	{
		__label = label;
	}

	/// <summary>
	/// Sets the angle to print the label at.  Makenet-specific. </summary>
	/// <param name="labelAngle"> the angle to print the label at. </param>
	public virtual void setLabelAngle(double labelAngle)
	{
		__labelAngle = labelAngle;
	}

	/// <summary>
	/// Sets the direction to print the label in.  Makenet-specific. </summary>
	/// <param name="labelDir"> the direction to print the label in. </param>
	public virtual void setLabelDirection(int labelDir)
	{
		__labelDir = labelDir;
	}

	/// <summary>
	/// Sets the link data for confluences. </summary>
	/// <param name="link"> the value to set the link data to. </param>
	public virtual void setLink(long link)
	{
		__link = link;
	}

	/// <summary>
	/// Sets the natural flow. </summary>
	/// <param name="naturalFlow"> value to put into the natural flow. </param>
	public virtual void setNaturalFlow(double naturalFlow)
	{
		__naturalFlow = naturalFlow;
	}

	/// <summary>
	/// Sets the point flow. </summary>
	/// <param name="pointFlow"> value to put in point flow. </param>
	public virtual void setPointFlow(double pointFlow)
	{
		__pointFlow = pointFlow;
	}

	/// <summary>
	/// Sets the ID from the net file.  Makenet-specific. </summary>
	/// <param name="netid"> the value to set the net file ID to. </param>
	public virtual void setNetID(string netid)
	{
		if (!string.ReferenceEquals(netid, null))
		{
			__netID = netid;
		}
	}

	/// <summary>
	/// Sets the node number in the reach. </summary>
	/// <param name="nodeInReachNum"> the node number in the reach. </param>
	public virtual void setNodeInReachNumber(int nodeInReachNum)
	{
		__nodeInReachNum = nodeInReachNum;
	}

	/// <summary>
	/// Sets the type of node this is. </summary>
	/// <param name="nodeType"> the type of node this is. </param>
	public virtual void setNodeType(string nodeType)
	{
		__nodeType = nodeType;
	}

	/// <summary>
	/// Sets this node's position on the network diagram. </summary>
	/// <param name="x"> the x location. </param>
	/// <param name="y"> the y location. </param>
	/// <param name="width"> the width of the node. </param>
	/// <param name="height"> the height of the node. </param>
	public virtual void setPosition(double x, double y, double width, double height)
	{
		__x = x;
		__y = y;
		__width = width;
		__height = height;
	}

	/// <summary>
	/// Sets the precip.  Makenet-specific. </summary>
	/// <param name="precip"> the precip to set. </param>
	public virtual void setPrecip(double precip)
	{
		__precip = precip;
	}

	/// <summary>
	/// Sets the precip as a String.  Makenet-specific. </summary>
	/// <param name="precipString"> value to set the precip string to. </param>
	public virtual void setPrecipString(string precipString)
	{
		if (!string.ReferenceEquals(precipString, null))
		{
			__precipString = precipString;
		}
	}

	/// <summary>
	/// Sets the gain proration factor.  Makenet-specific. </summary>
	/// <param name="prorationFactor"> value to set the proration factor to. </param>
	public virtual void setProrationFactor(double prorationFactor)
	{
		__prorationFactor = prorationFactor;
	}

	/// <summary>
	/// Sets the reach number. </summary>
	/// <param name="reachCounter"> value to set the reach number to. </param>
	public virtual void setReachCounter(int reachCounter)
	{
		__reachCounter = reachCounter;
	}

	/// <summary>
	/// Sets the reach level. </summary>
	/// <param name="reachLevel"> value to set the reach level to. </param>
	public virtual void setReachLevel(int reachLevel)
	{
		__reachLevel = reachLevel;
	}

	/// <summary>
	/// Sets whether this node's data was read from the database or not. </summary>
	/// <param name="read"> whether this node's data was read from the database. </param>
	public virtual void setReadFromDB(bool read)
	{
		__readFromDB = read;
	}

	/// <summary>
	/// Sets the right to be show in the node label. </summary>
	/// <param name="right"> the right information to show in the node label. </param>
	public virtual void setRight(string right)
	{
		__right = right;
	}

	/// <summary>
	/// Sets the river node ID. </summary>
	/// <param name="riverNodeID"> the value to set the river node ID to. </param>
	public virtual void setRiverNodeID(string riverNodeID)
	{
		if (!string.ReferenceEquals(riverNodeID, null))
		{
			__riverNodeID = riverNodeID;
		}
	}

	/// <summary>
	/// Sets whether this node was selected on the network diagram. </summary>
	/// <param name="selected"> whether this node was selected on the network diagram. </param>
	public virtual void setSelected(bool selected)
	{
		__isSelected = selected;
	}

	/// <summary>
	/// Sets the node count. </summary>
	/// <param name="serial"> the value to set the node count to. </param>
	public virtual void setSerial(int serial)
	{
		__serial = serial;
	}

	/// <summary>
	/// Sets whether to show call information. </summary>
	/// <param name="showCalls"> whether to show call information. </param>
	public virtual void setShowCalls(bool showCalls)
	{
		__showCalls = showCalls;
	}

	/// <summary>
	/// Sets whether to show the delivery flow. </summary>
	/// <param name="showDeliveryFlow"> whether to show the delivery flow. </param>
	public virtual void setShowDeliveryFlow(bool showDeliveryFlow)
	{
		__showDeliveryFlow = showDeliveryFlow;
	}

	/// <summary>
	/// Sets whether to show the natural flow. </summary>
	/// <param name="showNaturalFlow"> whether to show the natural flow. </param>
	public virtual void setShowNaturalFlow(bool showNaturalFlow)
	{
		__showNaturalFlow = showNaturalFlow;
	}

	/// <summary>
	/// Sets whether to show the point flow. </summary>
	/// <param name="showPointFlow"> whether to show the point flow. </param>
	public virtual void setShowPointFlow(bool showPointFlow)
	{
		__showPointFlow = showPointFlow;
	}

	/// <summary>
	/// Sets whether to show right information. </summary>
	/// <param name="showRights"> whether to show right information. </param>
	public virtual void setShowRights(bool showRights)
	{
		__showRights = showRights;
	}

	/// <summary>
	/// Sets the structure stream mile.  WIS-specific. </summary>
	/// <param name="streamMile"> value to set the stream mile to. </param>
	public virtual void setStreamMile(double streamMile)
	{
		__streamMile = streamMile;
	}

	/// <summary>
	/// Sets the stream number.  WIS-specific. </summary>
	/// <param name="streamNumber"> value to put into the stream number. </param>
	public virtual void setStreamNumber(long streamNumber)
	{
		__streamNum = streamNumber;
	}

	/// <summary>
	/// Sets the symbol to use for the node. </summary>
	/// <param name="symbol"> the GRSymbol to use. </param>
	public virtual void setSymbol(GRSymbol symbol)
	{
		__symbol = symbol;
	}

	/// <summary>
	/// Set the node symbol given the node type. </summary>
	/// <param name="nodeType"> the node type to be used to determine the symbol. </param>
	/// <param name="computeSize"> if true the size is computed. </param>
	private void setSymbolFromNodeType(int nodeType, bool computeSize)
	{
		int symbolStyle = GRSymbol.TYPE_POLYGON;
		GRColor symbolColor = GRColor.black;
		int iconDiameter = getIconDiameter();
		if ((nodeType == NODE_TYPE_CONFLUENCE) || (nodeType == NODE_TYPE_XCONFLUENCE))
		{
			__symbol = new GRSymbol(GRSymbol.SYM_FCIR, symbolStyle, symbolColor, symbolColor, iconDiameter / 2, iconDiameter / 2);
			__symText = null;
			if (computeSize)
			{
				__width /= 2;
				__height /= 2;
			}
		}
		else if (nodeType == NODE_TYPE_DIV)
		{
			__symbol = new GRSymbol(GRSymbol.SYM_CIR, symbolStyle, symbolColor, symbolColor, iconDiameter, iconDiameter);
			__symText = "D";
		}
		else if (nodeType == NODE_TYPE_DIV_AND_WELL)
		{
			__symbol = new GRSymbol(GRSymbol.SYM_CIR, symbolStyle, symbolColor, symbolColor, iconDiameter, iconDiameter);
			__symText = "DW";
		}
		else if (nodeType == NODE_TYPE_END)
		{
			__symbol = new GRSymbol(GRSymbol.SYM_EX, symbolStyle, symbolColor, symbolColor, iconDiameter, iconDiameter);
			__secondarySymbol = new GRSymbol(GRSymbol.SYM_CIR, symbolStyle, symbolColor, symbolColor, iconDiameter, iconDiameter);
			__symText = null;
		}
		else if (nodeType == NODE_TYPE_FLOW)
		{
			__symbol = new GRSymbol(GRSymbol.SYM_FCIR, symbolStyle, symbolColor, symbolColor, iconDiameter, iconDiameter);
			__symText = null;
		}
		else if (nodeType == NODE_TYPE_ISF)
		{
			__symbol = new GRSymbol(GRSymbol.SYM_CIR, symbolStyle, symbolColor, symbolColor, iconDiameter, iconDiameter);
			//__symText = "I";
			__symText = "M";
		}
		else if (nodeType == NODE_TYPE_LABEL)
		{
			__symbol = null;
			__symText = null;
		}
		else if (nodeType == NODE_TYPE_OTHER)
		{
			__symbol = new GRSymbol(GRSymbol.SYM_CIR, symbolStyle, symbolColor, symbolColor, iconDiameter, iconDiameter);
			__symText = "O";
		}
		else if (nodeType == NODE_TYPE_PLAN)
		{
			__symbol = new GRSymbol(GRSymbol.SYM_FCIR, symbolStyle, GRColor.lightGray, symbolColor, iconDiameter, iconDiameter);
			__symText = "PL";
		}
		else if (nodeType == NODE_TYPE_RES)
		{
			__symbol = new GRSymbol(GRSymbol.SYM_FRTRI, symbolStyle, symbolColor, symbolColor, iconDiameter, iconDiameter);
			__symText = null;
		}
		else if (nodeType == NODE_TYPE_WELL)
		{
			__symbol = new GRSymbol(GRSymbol.SYM_CIR, symbolStyle, symbolColor, symbolColor, iconDiameter, iconDiameter);
			__symText = "W";
		}
		else
		{
			Message.printWarning(2, "", "Unknown symbol for node type " + nodeType);
			__symbol = null;
			__symText = null;
		}
	}

	/// <summary>
	/// Sets the tributary number. </summary>
	/// <param name="tributaryNumber"> value to put into tributary number. </param>
	public virtual void setTributaryNumber(int tributaryNumber)
	{
		__tributaryNum = tributaryNumber;
	}

	/// <summary>
	/// Sets the node type. </summary>
	/// <param name="type"> value to set the node type to. </param>
	public virtual void setType(int type)
	{
		__type = type;
	}

	/// <summary>
	/// Sets the node type based on the abbreviated String type.  The abbreviated
	/// string is used in the StateMod station names and the network diagram. </summary>
	/// <param name="type"> the abbreviated string specifying what type of node this node is. </param>
	public virtual void setTypeAbbreviation(string type)
	{
		if (type.Equals("BLK", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_BLANK);
		}
		else if (type.Equals("DIV", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_DIV);
		}
		else if (type.Equals("D&W", StringComparison.OrdinalIgnoreCase) || type.Equals("DW", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_DIV_AND_WELL);
		}
		else if (type.Equals("WEL", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_WELL);
		}
		else if (type.Equals("FLO", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_FLOW);
		}
		else if (type.Equals("CON", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_CONFLUENCE);
		}
		else if (type.Equals("ISF", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_ISF);
		}
		else if (type.Equals("RES", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_RES);
		}
		else if (type.Equals("IMP", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_IMPORT);
		}
		else if (type.Equals("BFL", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_BASEFLOW);
		}
		else if (type.Equals("END", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_END);
		}
		else if (type.Equals("OTH", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_OTHER);
		}
		else if (type.Equals("UNK", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_UNKNOWN);
		}
		else if (type.Equals("STR", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_STREAM);
		}
		else if (type.Equals("LAB", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_LABEL);
		}
		else if (type.Equals("FOR", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_FORMULA);
		}
		else if (type.Equals("XCN", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_XCONFLUENCE);
		}
		else if (type.Equals("LBN", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_LABEL_NODE);
		}
		else if (type.Equals("PLN", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_PLAN);
		}
	}

	/// <summary>
	/// Sets the list of nodes upstream of this node.  Used by the network diagramming tools. </summary>
	/// <param name="v"> the list of upstream nodes.  If null, then there are no nodes upstream of this node. </param>
	public virtual void setUpstreamNodes(IList<HydrologyNode> v)
	{
		if (v == null)
		{
			__upstream = new List<HydrologyNode>();
		}
		else
		{
			__upstream = v;
		}
	}

	/// <summary>
	/// Sets the user description.  Makenet-specific. </summary>
	/// <param name="desc"> value to put in the user description. </param>
	public virtual void setUserDescription(string desc)
	{
		if (string.ReferenceEquals(desc, null))
		{
			__userDesc = "";
		}
		else
		{
			__userDesc = desc;
		}
	}

	/// <summary>
	/// Set the type from a string.  This is used in CWRAT WIS. </summary>
	/// <param name="type"> the type to set. </param>
	public virtual void setType(string type)
	{
		if (type.Equals("diversion", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_DIV);
		}
		else if (type.Equals("well", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_WELL);
		}
		else if (type.Equals("confluence", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_CONFLUENCE);
		}
		else if (type.Equals("formula", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_FORMULA);
		}
		else if (type.Equals("minflow", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_ISF);
		}
		else if (type.Equals("other", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_OTHER);
		}
		else if (type.Equals("reservoir", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_RES);
		}
		else if (type.Equals("station", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_FLOW);
		}
		else if (type.Equals("stream", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_STREAM);
		}
		else if (type.Equals("string", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_LABEL);
		}
		else if (type.Equals("labelNode", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_LABEL_NODE);
		}
		else if (type.Equals("Plan", StringComparison.OrdinalIgnoreCase))
		{
			setType(NODE_TYPE_PLAN);
		}
	}

	/// <summary>
	/// Sets how upstream nodes are constructed.  See TRIBS_*. </summary>
	/// <param name="upstreamOrder"> how upstream nodes are constructed. </param>
	public virtual void setUpstreamOrder(int upstreamOrder)
	{
		__upstreamOrder = upstreamOrder;
	}

	/// <summary>
	/// Returns the verbose node type description string. </summary>
	/// <param name="type"> the type of node for which to return the verbose type. </param>
	/// <returns> the verbose node type description string. </returns>
	public virtual void setVerboseType(string stype)
	{
		// Abbreviations...
		if (stype.Equals("Blank", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_BLANK;
		}
		else if (stype.Equals("Diversion", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_DIV;
		}
		else if (stype.Equals("Diversion and Well", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_DIV_AND_WELL;
		}
		else if (stype.Equals("Well", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_WELL;
		}

	// The following is done because flow types have different names in WIS
	// and non-WIS screens.  Unfortunately, this causes a lot of problems.  
	// A far far better solution would be to rename one of the flow types, 
	// e.g. for WIS make a NODE_TYPE_STATION node that uses "Station" and leave
	// NODE_TYPE_FLOW for "Streamflow".  There's lots of work to be done in 
	// revising these classes, though.  It'll have to wait.

		// FIXME SAM 2008-03-15 Need to remove WIS from this general class
		else if (stype.Equals("Streamflow", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_FLOW;
		}
		// FIXME SAM 2008-03-15 Need to remove WIS from this general class
		else if (stype.Equals("Station", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_FLOW;
		}
		else if (stype.Equals("Confluence", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_CONFLUENCE;
		}
		else if (stype.Equals("Instream Flow", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_ISF;
		}
		else if (stype.Equals("Reservoir", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_RES;
		}
		else if (stype.Equals("Import", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_IMPORT;
		}
		else if (stype.Equals("Baseflow", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_BASEFLOW;
		}
		else if (stype.Equals("End", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_END;
		}
		else if (stype.Equals("Other", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_OTHER;
		}
		else if (stype.Equals("Uknown", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_UNKNOWN;
		}
		else if (stype.Equals("Stream", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_STREAM;
		}
		else if (stype.Equals("Label", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_LABEL;
		}
		else if (stype.Equals("Formula", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_FORMULA;
		}
		else if (stype.Equals("XConfluence", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_XCONFLUENCE;
		}
		else if (stype.Equals("LabelNode", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_LABEL_NODE;
		}
		else if (stype.Equals("Plan", StringComparison.OrdinalIgnoreCase))
		{
			__type = NODE_TYPE_PLAN;
		}
		else
		{
			Message.printWarning(2, "setVerboseType", "Unknown type: '" + stype + "'");
			__type = NODE_TYPE_BLANK;
		}
	}

	/// <summary>
	/// Sets whether this node is visible or not. </summary>
	/// <param name="visible"> whether this node is visible or not. </param>
	public virtual void setVisible(bool visible)
	{
		__visible = visible;
	}

	/// <summary>
	/// Sets area * precip.  Makenet-specific. </summary>
	/// <param name="water"> the value to put in area * precip. </param>
	public virtual void setWater(double water)
	{
		__water = water;
	}

	/// <summary>
	/// Sets area * precip as a String.  Makenet-specific. </summary>
	/// <param name="waterString"> value of area * precip as a String. </param>
	public virtual void setWaterString(string waterString)
	{
		if (!string.ReferenceEquals(waterString, null))
		{
			__waterString = waterString;
		}
	}

	/// <summary>
	/// Sets the wis format associated with this node. </summary>
	/// <param name="wisFormat"> the wis format associated with this node. </param>
	/*
	public void setWISFormat(HydroBase_WISFormat wisFormat) {
		__wisFormat = wisFormat;
	}
	*/
	//

	/// <summary>
	/// Sets the number of the wis sheet this node is associated with. </summary>
	/// <param name="wis_num"> the number of the wis sheet this node is associated with. </param>
	/*
	public void setWis_num(int wis_num) {
		__wisNum = wis_num;
	}
	*/
	//FIXME SAM 2008-03-15 Need to remove WIS from this general class

	/// <summary>
	/// Sets the node X coordinate. </summary>
	/// <param name="x"> value to put into X </param>
	public virtual void setX(double x)
	{
		__x = x;
	}

	/// <summary>
	/// Sets the node Y coordinate. </summary>
	/// <param name="y"> value to put into Y. </param>
	public virtual void setY(double y)
	{
		__y = y;
	}

	/// <summary>
	/// Returns whether calls should be shown. </summary>
	/// <returns> whether calls should be shown. </returns>
	public virtual bool showCalls()
	{
		return __showCalls;
	}

	/// <summary>
	/// Returns whether the delivery flow should be shown. </summary>
	/// <returns> whether the delivery flow should be shown. </returns>
	public virtual bool showDeliveryFlow()
	{
		return __showDeliveryFlow;
	}

	/// <summary>
	/// Returns whether the natural flow should be shown. </summary>
	/// <returns> whether the natural flow should be shown. </returns>
	public virtual bool showNaturalFlow()
	{
		return __showNaturalFlow;
	}

	/// <summary>
	/// Returns whether the point flow should be shown. </summary>
	/// <returns> whether the point flow should be shown. </returns>
	public virtual bool showPointFlow()
	{
		return __showPointFlow;
	}

	/// <summary>
	/// Returns whether to show the rights. </summary>
	/// <returns> whether the rights should be shown. </returns>
	public virtual bool showRights()
	{
		return __showRights;
	}

	/// <summary>
	/// Returns a String representation of the object suitable for debugging a network. </summary>
	/// <returns> a String representation of the object suitable for debugging a network. </returns>
	public virtual string toNetworkDebugString()
	{
		string down = "";
		string up = "";

		if (__downstream != null)
		{
			down = __downstream.getCommonID();
		}
		else
		{
			down = "null";
		}

		if (__upstream != null)
		{
			for (int i = 0; i < getNumUpstreamNodes(); i++)
			{
				up = up + " [" + i + "]:\"" + getUpstreamNode(i).getCommonID() + "\"";
			}
		}
		else
		{
			up = "null";
		}

		return "[" + __label + "]  US: '" + up + "'   DS: '" + down + "'" + "  RL: " + __reachLevel;
	}

	/// <summary>
	/// Returns information about the node in a format useful for debugging node layouts in the network editor. </summary>
	/// <returns> information about the node in a format useful for debugging node layouts in the network editor. </returns>
	public virtual string toNetworkString()
	{
		return "Identifier: '" + __identifier + "'\n" +
			"Node type:  '" + __nodeType + "'\n" +
			"X:           " + __x + "\n" +
			"DBX:         " + __dbX + "\n" +
			"Y:           " + __y + "\n" +
			"DBY:         " + __dbY + "\n" +
			"Description:'" + __desc + "'\n" +
			"User desc:  '" + __userDesc + "'\n" +
			"CommonID:   '" + __commonID + "'\n" +
			"IsVisible:   " + __visible + "\n";
	}

	/// <summary>
	/// Returns a verbose String representation of the object. </summary>
	/// <returns> a verbose String representation of the object. </returns>
	public override string ToString()
	{
		string down = "";
		string up = "";

		if (__downstream != null)
		{
			down = __downstream.getCommonID();
		}
		else
		{
			down = "null";
		}

		if (__upstream != null)
		{
			for (int i = 0; i < getNumUpstreamNodes(); i++)
			{
				up = up + " [" + i + "]:\"" + getUpstreamNode(i).getCommonID() + "\"";
			}
		}
		else
		{
			up = "null";
		}

		return "\"" + getCommonID() + "\" T=" + getTypeString(__type,1) + " T#=" + __tributaryNum + " RC=" + __reachCounter + " RL="
			+ __reachLevel + " #=" + __serial + " #inR="
			+ __nodeInReachNum + " CO=" + __computationalOrder + " DWN=\"" + down + "\" #up=" + getNumUpstreamNodes() + " UP="
			+ up;
	}

	/// <summary>
	/// Returns a String representation of the DB table data from the node. </summary>
	/// <returns> a String representation of the DB table data from the node. </returns>
	public virtual string toTableString()
	{
		return "Wis_num:     " + __wisNum + "\n" +
			"Identifier: '" + __identifier + "'\n" +
			"Node type:  '" + __nodeType + "'\n" +
			"X:           " + __x + "\n" +
			"DBX:         " + __dbX + "\n" +
			"Y:           " + __y + "\n" +
			"DBY:         " + __dbY + "\n" +
			"Label:      '" + __label;
	}

	/// <summary>
	/// Writes the node out to the given PrintWriter as XML. </summary>
	/// <param name="out"> the PrintWrite to write the node out to. </param>
	/// <param name="verbose"> if true, then all the information about the node will be written out. </param>
	public virtual string writeNodeXML(PrintWriter @out, bool verbose)
	{
		string xml = "    <Node ";
		string n = System.getProperty("line.separator");

		string id = getCommonID();
		id = StringUtil.replaceString(id, "&", "&amp;");
		id = StringUtil.replaceString(id, "<", "&lt;");
		id = StringUtil.replaceString(id, ">", "&gt;");

		xml += "ID = \"" + id + "\"" + n;
		xml += "         AlternateX = \"" + __dbX + "\"" + n;
		xml += "         AlternateY = \"" + __dbY + "\"" + n;
		string desc = __desc;
		desc = StringUtil.replaceString(desc, "&", "&amp;");
		desc = StringUtil.replaceString(desc, "<", "&lt;");
		desc = StringUtil.replaceString(desc, ">", "&gt;");
		xml += "         Description = \"" + desc + "\"" + n;
	//	xml += "         Identifier = \"" + __identifier + "\"" + n;
		// FIXME SAM 2008-12-10 Need to convert to IsNaturalFlow when old version of StateDMI can
		// be phased out.  For now write both with the same value so that the network will work with
		// old and new software.
		xml += "         IsBaseflow = \"" + __isNaturalFlow + "\"" + n;
		xml += "         IsNaturalFlow = \"" + __isNaturalFlow + "\"" + n;
		xml += "         IsImport = \"" + __isImport + "\"" + n;
	//	xml += "         LabelAngle = \"" + __labelAngle + "\"" + n;

		if (__isNaturalFlow)
		{
			xml += "         Area = \"" + __area + "\"" + n;
			xml += "         Precipitation = \"" + __precip + "\"" + n;
		}

		string sdir = null;
		int dir = getLabelDirection();
		dir = dir % 10;

		if (dir == 1)
		{
			sdir = "AboveCenter";
		}
		else if (dir == 7)
		{
			sdir = "UpperRight";
		}
		else if (dir == 4)
		{
			sdir = "Right";
		}
		else if (dir == 8)
		{
			sdir = "LowerRight";
		}
		else if (dir == 2)
		{
			sdir = "BelowCenter";
		}
		else if (dir == 5)
		{
			sdir = "LowerLeft";
		}
		else if (dir == 3)
		{
			sdir = "Left";
		}
		else if (dir == 6)
		{
			sdir = "UpperLeft";
		}
		else if (dir == 9)
		{
			sdir = "Center";
		}
		else
		{
			sdir = "BelowCenter";
		}

		if (!string.ReferenceEquals(sdir, null))
		{
			xml += "         LabelPosition = \"" + sdir + "\"" + n;
		}

		if (getType() == NODE_TYPE_RES)
		{
			sdir = null;
			dir = getLabelDirection();
			dir = dir / 10;
			if (dir == 2)
			{
				sdir = "Up";
			}
			else if (dir == 1)
			{
				sdir = "Down";
			}
			else if (dir == 4)
			{
				sdir = "Left";
			}
			else if (dir == 3)
			{
				sdir = "Right";
			}
			else
			{
				sdir = "Left";
			}
			if (!string.ReferenceEquals(sdir, null))
			{
				xml += "         ReservoirDir = \"" + sdir + "\"" + n;
			}
		}

	//	String type = getTypeString(__type, 1);
		string type = getVerboseType(__type);
	//	if (type.equalsIgnoreCase("D&W")) {
	//		type = "DW";
	//	}
		xml += "         Type = \"" + type + "\"" + n;

		if (verbose)
		{
			xml += "         ComputationalOrder = \"" + __computationalOrder + "\"" + n;
			xml += "         NodeInReachNum = \"" + __nodeInReachNum + "\""
				+ "" + n;
			xml += "        ReachCounter = \"" + __reachCounter + "\"" + n;
			xml += "         Serial = \"" + __serial + "\"" + n;
			xml += "         TributaryNum = \"" + __tributaryNum + "\"" + n;
			xml += "         UpstreamOrder = \"" + __upstreamOrder + "\""
				+ n;
		}

		xml += "         X = \"" + StringUtil.formatString(__x, "%13.6f").Trim() + "\"" + n;
		xml += "         Y = \"" + StringUtil.formatString(__y, "%13.6f").Trim() + "\">" + n;

		if (__downstream != null)
		{
			string down = __downstream.getCommonID();
			down = StringUtil.replaceString(down, "&", "&amp;");
			down = StringUtil.replaceString(down, "<", "&lt;");
			down = StringUtil.replaceString(down, ">", "&gt;");
			xml += "        <DownstreamNode ID = \"" + down + "\"/>" + n;
		}

		string up = "";
		int num = getNumUpstreamNodes();
		if (__upstream != null)
		{
			for (int i = 0; i < num; i++)
			{
				up = getUpstreamNode(i).getCommonID();
				up = StringUtil.replaceString(up, "&", "&amp;");
				up = StringUtil.replaceString(up, "<", "&lt;");
				up = StringUtil.replaceString(up, ">", "&gt;");
				xml += "        <UpstreamNode ID = \"" + up + "\"/>" + n;
			}
		}
		xml += "    </Node>" + n;

		if (@out != null)
		{
			@out.print(xml + "" + n);
			return null;
		}
		else
		{
			return xml;
		}
	}

	}

}