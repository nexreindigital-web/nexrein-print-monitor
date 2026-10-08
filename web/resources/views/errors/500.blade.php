@extends('errors.layout')

@section('title', '500 — Server Error')
@section('code', '500')
@section('icon', '⚡')
@section('icon_style', 'background: rgba(244, 63, 94, 0.15); border-color: rgba(244, 63, 94, 0.3);')
@section('heading', 'Internal Server Error')
@section('message', 'An unexpected error occurred while processing your print database request. Our system diagnostics have logged this event.')
