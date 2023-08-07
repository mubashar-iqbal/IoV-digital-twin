//SPDX-License-Identifier: UNLICENSED
pragma solidity ^0.8.9;

contract VehicleAnalyzer{
    uint256 minTemp;
    uint256 maxTemp;
    
    constructor () {
        minTemp = 65;
        maxTemp = 75;
    }

    function getTempThresholds() public view returns (uint256, uint256) {

        return (minTemp, maxTemp);
    }
   
}