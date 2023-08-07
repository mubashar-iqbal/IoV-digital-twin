//SPDX-License-Identifier: UNLICENSED


pragma solidity ^0.8.9;

contract Vehicle {

    int private temperature;

    event TemperatureSet(address setter, int temperature);


    function setTemp(int _temp) public {
        temperature = _temp;
        emit TemperatureSet(msg.sender, temperature);
    }

    function getTemp() public view returns (int) {
        return temperature;
    }
   
}