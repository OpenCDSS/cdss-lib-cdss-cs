// UpstreamFlowNodeI - interface to define upstream flow node in network

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

namespace cdss.domain.hydrology.network
{
	/// <summary>
	/// Interface to allow an application to indicate whether a node should be treated
	/// as the upstream flow node for a requested node.  This interface is used to connect the more
	/// generic HydroBase_NodeNetwork.findUpstreamFlowNodes() method with the StateMod_PrfGageData
	/// instances used by StateDMI when processing the stream estimate coefficient data.  Using this
	/// interface allows HydrologyNodeNetwork to NOT import StateMod package resources.
	/// </summary>
	public interface UpstreamFlowNodeI
	{

		/// <summary>
		/// Checks to see if the node is one where a "neighboring gage" is to be used for the upstream
		/// flow node.  See the StateDMI SetStreamEstimateCoefficientsPFGage() command. </summary>
		/// <param name="commonID"> the id of the node to check </param>
		/// <returns> the number of the node if it is to receive information, or -1 if
		/// the node wasn't found or is not to receive information. </returns>
		int isSetprfTarget(string commonID);
	}

}