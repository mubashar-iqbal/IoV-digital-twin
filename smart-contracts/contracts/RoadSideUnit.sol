//SPDX-License-Identifier: UNLICENSED
pragma solidity ^0.8.9;


import "./AnalyzerVehicle.sol";
import "./Vehicle.sol";


contract RoadSideUnit {

    address public vehicleAnalyzerAddr;
    address public vehicleAddr;
    

    constructor() {

        Vehicle vehicle = new Vehicle();
        vehicleAddr = address(vehicle);

        VehicleAnalyzer vehicleAnalyzer = new VehicleAnalyzer();
        vehicleAnalyzerAddr = address(vehicleAnalyzer);
    }

    function getChildContractAddresses() public view returns (address, address) {

        return (vehicleAddr, vehicleAnalyzerAddr);
    }
   
}